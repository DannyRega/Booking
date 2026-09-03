using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

public interface IBookingRepository
{
    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);

    // Bloqueo pesimista nativo para alta concurrencia
    Task<Session?> GetSessionForUpdateAsync(int sessionId, CancellationToken ct = default);

    Task<int> CountBookingsBySessionIdAsync(int sessionId, CancellationToken ct = default);
    Task<bool> HasUserBookingAsync(int sessionId, int userId, CancellationToken ct = default);
    Task<bool> HasOverlappingBookingAsync(int userId, DateTime start, DateTime end, CancellationToken ct = default);
    Task AddBookingAsync(Booking booking, CancellationToken ct = default);

    Task<Booking?> GetBookingWithSessionAsync(int bookingId, CancellationToken ct = default);
    Task DeleteBookingAsync(Booking booking, CancellationToken ct = default);

    Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, CancellationToken ct = default);
    Task SaveIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}