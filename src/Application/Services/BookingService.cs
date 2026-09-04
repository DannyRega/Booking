using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;
/// <summary>
/// service class for managing bookings, including creating and canceling bookings with idempotency support.
/// </summary>
/// <param name="repository"></param>
public class BookingService(IBookingRepository repository)
{
    /// <summary>
    /// Creates a new booking for a user in a specific session, ensuring idempotency and handling various business rules such as capacity limits and overlapping bookings.
    /// </summary>
    /// <param name="userId">The ID of the user creating the booking.</param>
    /// <param name="sessionId">The ID of the session for which to create the booking.</param>
    /// <param name="idempotencyKey">The key for ensuring idempotency.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A tuple containing the HTTP status code and the response body.</returns>
    /// <exception cref="NotFoundException"></exception>
    public async Task<(int StatusCode, object Body)> CreateBookingAsync(
        int userId,
        int sessionId,
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingRecord = await repository.GetIdempotencyRecordAsync(idempotencyKey, ct);
            if (existingRecord is not null)
            {
                var cachedObj = JsonSerializer.Deserialize<object>(existingRecord.ResponseBody);
                return (existingRecord.StatusCode, cachedObj!);
            }
        }

        var alreadyBooked = await repository.HasUserBookingAsync(sessionId, userId, ct);
        if (alreadyBooked)
        {
            var errorBody = new { error = "Ya tienes una reserva para esta sesión." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            return (409, errorBody);
        }

        await using var tx = await repository.BeginTransactionAsync(ct);

        var session = await repository.GetSessionForUpdateAsync(sessionId, ct)
            ?? throw new NotFoundException("La sesión no existe.");

        var currentBookings = await repository.CountBookingsBySessionIdAsync(sessionId, ct);
        if (currentBookings >= session.Capacity)
        {
            await tx.RollbackAsync(ct);

            var errorBody = new { error = "No hay cupos disponibles para esta sesión." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            return (409, errorBody);
        }

        var sessionEnd = session.StartsAt.AddMinutes(session.DurationMinutes);
        var hasOverlap = await repository.HasOverlappingBookingAsync(userId, session.StartsAt, sessionEnd, ct);
        if (hasOverlap)
        {
            await tx.RollbackAsync(ct);

            var errorBody = new { error = "Tienes otra sesión que se solapa en este horario." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            return (409, errorBody);
        }

        var booking = new Booking
        {
            SessionId = sessionId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddBookingAsync(booking, ct);
        await repository.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        var resultDto = new BookingResponseDto(booking.Id, booking.SessionId, booking.UserId, booking.CreatedAt);
        await SaveIdempotencyIfPresentAsync(idempotencyKey, 201, resultDto, ct);

        return (201, resultDto);
    }
    /// <summary>
    /// Cancels an existing booking for a user.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to cancel.</param>
    /// <param name="userId">The ID of the user canceling the booking.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="NotFoundException"></exception>
    /// <exception cref="ForbiddenException"></exception>
    /// <exception cref="ConflictException"></exception>
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
    /// <summary>
    /// Saves an idempotency record if the provided key is not null or whitespace. This method serializes the response body and stores it along with the status code and creation timestamp in the repository.
    /// </summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="status">The HTTP status code.</param>
    /// <param name="body">The response body.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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