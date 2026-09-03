using Application.Interfaces;
using Application.DTOs;
using Domain.Entities ;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.Services;

public class BookingService(IApplicationDbContext context)
{
    public async Task<(int StatusCode, object Body)> CreateBookingAsync(
        int userId,
        int sessionId,
        string? idempotencyKey,
        CancellationToken ct)
    {
        // 1. Manejo de Idempotencia
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingRecord = await context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == idempotencyKey, ct);

            if (existingRecord is not null)
            {
                var cachedObj = JsonSerializer.Deserialize<object>(existingRecord.ResponseBody);
                return (existingRecord.StatusCode, cachedObj!);
            }
        }

        // 2. Transacción Atómica con SELECT ... FOR UPDATE
        await using var tx = await context.BeginTransactionAsync(ct);

        // Bloqueo pesimista de fila en Postgres: Evita condición de carrera
        var session = await context.Sessions
            .FromSqlInterpolated($"SELECT * FROM sessions WHERE id = {sessionId} FOR UPDATE")
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("La sesión no existe.");

        // Validar capacidad
        var currentBookingsCount = await context.Bookings
            .CountAsync(b => b.SessionId == sessionId, ct);

        if (currentBookingsCount >= session.Capacity)
        {
            var errorBody = new { error = "No hay cupos disponibles para esta sesión." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            await tx.CommitAsync(ct);
            return (409, errorBody);
        }

        // Validar no duplicado
        var alreadyBooked = await context.Bookings
            .AnyAsync(b => b.SessionId == sessionId && b.UserId == userId, ct);

        if (alreadyBooked)
        {
            var errorBody = new { error = "Ya tienes una reserva para esta sesión." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            await tx.CommitAsync(ct);
            return (409, errorBody);
        }

        // Validar solapamiento de horarios
        var sessionEnd = session.StartsAt.AddMinutes(session.DurationMinutes);

        var hasOverlap = await context.Bookings
            .Include(b => b.Session)
            .Where(b => b.UserId == userId)
            .AnyAsync(b =>
                b.Session!.StartsAt < sessionEnd &&
                session.StartsAt < b.Session.StartsAt.AddMinutes(b.Session.DurationMinutes), ct);

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

        context.Bookings.Add(booking);
        await context.SaveChangesAsync(ct);

        var resultDto = new BookingResponseDto(booking.Id, booking.SessionId, booking.UserId, booking.CreatedAt);
        await SaveIdempotencyIfPresentAsync(idempotencyKey, 201, resultDto, ct);

        await tx.CommitAsync(ct);
        return (201, resultDto);
    }

    public async Task CancelBookingAsync(int bookingId, int userId, CancellationToken ct)
    {
        var booking = await context.Bookings
            .Include(b => b.Session)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new NotFoundException("Reserva no encontrada.");

        if (booking.UserId != userId)
            throw new ForbiddenException("No tienes permiso para cancelar esta reserva.");

        if (booking.Session!.StartsAt - DateTime.UtcNow < TimeSpan.FromHours(2))
            throw new ConflictException("No se puede cancelar con menos de 2 horas de anticipación.");

        context.Bookings.Remove(booking);
        await context.SaveChangesAsync(ct);
    }

    private async Task SaveIdempotencyIfPresentAsync(string? key, int status, object body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        context.IdempotencyRecords.Add(new Domain.Entities.IdempotencyRecord
        {
            Key = key,
            StatusCode = status,
            ResponseBody = JsonSerializer.Serialize(body),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(ct);
    }
}