using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

public class BookingService(IBookingRepository repository)
{
    public async Task<(int StatusCode, object Body)> CreateBookingAsync(
        int userId,
        int sessionId,
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        // 1. Idempotencia previa
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingRecord = await repository.GetIdempotencyRecordAsync(idempotencyKey, ct);
            if (existingRecord is not null)
            {
                var cachedObj = JsonSerializer.Deserialize<object>(existingRecord.ResponseBody);
                return (existingRecord.StatusCode, cachedObj!);
            }
        }

        // 2. Transacción Atómica con Bloqueo Pesimista
        await using var tx = await repository.BeginTransactionAsync(ct);

        var session = await repository.GetSessionForUpdateAsync(sessionId, ct)
            ?? throw new NotFoundException("La sesión no existe.");

        // Validar capacidad
        var currentBookings = await repository.CountBookingsBySessionIdAsync(sessionId, ct);
        if (currentBookings >= session.Capacity)
        {
            var errorBody = new { error = "No hay cupos disponibles para esta sesión." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            await tx.CommitAsync(ct);
            return (409, errorBody);
        }

        // Validar no duplicado
        var alreadyBooked = await repository.HasUserBookingAsync(sessionId, userId, ct);
        if (alreadyBooked)
        {
            var errorBody = new { error = "Ya tienes una reserva para esta sesión." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            await tx.CommitAsync(ct);
            return (409, errorBody);
        }

        // Validar solapamiento de horarios
        var sessionEnd = session.StartsAt.AddMinutes(session.DurationMinutes);
        var hasOverlap = await repository.HasOverlappingBookingAsync(userId, session.StartsAt, sessionEnd, ct);
        if (hasOverlap)
        {
            var errorBody = new { error = "Tienes otra sesión que se solapa en este horario." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            await tx.CommitAsync(ct);
            return (409, errorBody);
        }

        // Crear la reserva
        var booking = new Booking
        {
            SessionId = sessionId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddBookingAsync(booking, ct);
        await repository.SaveChangesAsync(ct);

        var resultDto = new BookingResponseDto(booking.Id, booking.SessionId, booking.UserId, booking.CreatedAt);
        await SaveIdempotencyIfPresentAsync(idempotencyKey, 201, resultDto, ct);

        await tx.CommitAsync(ct);
        return (201, resultDto);
    }

    public async Task CancelBookingAsync(int bookingId, int userId, CancellationToken ct = default)
    {
        var booking = await repository.GetBookingWithSessionAsync(bookingId, ct)
            ?? throw new NotFoundException("Reserva no encontrada.");

        if (booking.UserId != userId)
            throw new ForbiddenException("No tienes permiso para cancelar esta reserva.");

        if (booking.Session!.StartsAt - DateTime.UtcNow < TimeSpan.FromHours(2))
            throw new ConflictException("No se puede cancelar con menos de 2 horas de anticipación.");

        await repository.DeleteBookingAsync(booking, ct);
        await repository.SaveChangesAsync(ct);
    }

    private async Task SaveIdempotencyIfPresentAsync(string? key, int status, object body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        await repository.SaveIdempotencyRecordAsync(new Domain.Entities.IdempotencyRecord
        {
            Key = key,
            StatusCode = status,
            ResponseBody = JsonSerializer.Serialize(body),
            CreatedAt = DateTime.UtcNow
        }, ct);
        await repository.SaveChangesAsync(ct);
    }
}