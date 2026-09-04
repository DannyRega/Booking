using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;
/// <summary>
/// Service responsible for managing bookings, including creating and canceling bookings while ensuring business rules are enforced, such as session capacity, user booking conflicts, and idempotency of requests.
/// </summary>
/// <param name="repository">The repository for managing booking data.</param>
public class BookingService(IBookingRepository repository)
{
    /// <summary>
    /// Creates a booking for a user in a specific session, ensuring that the session has available capacity, the user does not already have a booking for that session, and that there are no overlapping bookings for the user. The method also supports idempotency to prevent duplicate bookings.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to create the booking.</param>
    /// <param name="sessionId">The ID of the session for which to create the booking.</param>
    /// <param name="idempotencyKey">An optional key to ensure idempotent requests.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

        await using var tx = await repository.BeginTransactionAsync(ct);

        var session = await repository.GetSessionForUpdateAsync(sessionId, ct)
            ?? throw new NotFoundException("La sesión no existe.");

        var currentBookings = await repository.CountBookingsBySessionIdAsync(sessionId, ct);
        if (currentBookings >= session.Capacity)
        {
            var errorBody = new { error = "No hay cupos disponibles para esta sesión." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            await tx.CommitAsync(ct);
            return (409, errorBody);
        }

        var alreadyBooked = await repository.HasUserBookingAsync(sessionId, userId, ct);
        if (alreadyBooked)
        {
            var errorBody = new { error = "Ya tienes una reserva para esta sesión." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            await tx.CommitAsync(ct);
            return (409, errorBody);
        }

        var sessionEnd = session.StartsAt.AddMinutes(session.DurationMinutes);
        var hasOverlap = await repository.HasOverlappingBookingAsync(userId, session.StartsAt, sessionEnd, ct);
        if (hasOverlap)
        {
            var errorBody = new { error = "Tienes otra sesión que se solapa en este horario." };
            await SaveIdempotencyIfPresentAsync(idempotencyKey, 409, errorBody, ct);
            await tx.CommitAsync(ct);
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

        var resultDto = new BookingResponseDto(booking.Id, booking.SessionId, booking.UserId, booking.CreatedAt);
        await SaveIdempotencyIfPresentAsync(idempotencyKey, 201, resultDto, ct);

        await tx.CommitAsync(ct);
        return (201, resultDto);
    }
    /// <summary>
    /// Cancels a booking for a user, ensuring that the booking exists, the user has permission to cancel it, and that the cancellation is made at least 2 hours before the session starts.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to cancel.</param>
    /// <param name="userId">The ID of the user who wants to cancel the booking.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="NotFoundException">Thrown when the booking is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when the user does not have permission to cancel the booking.</exception>
    /// <exception cref="ConflictException">Thrown when the cancellation is made too close to the session start time.</exception>
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
    /// <param name="ct">A cancellation token.</param>
    /// <returns></returns>
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