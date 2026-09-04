using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
/// <summary>
/// Represents a repository for managing bookings, sessions, and idempotency records in the application.
/// </summary>
/// <param name="context">The application's database context.</param>
public class BookingRepository(ApplicationDbContext context) : IBookingRepository
{
    /// <summary>
    /// Begins a new database transaction. If the database is in-memory, a null transaction adapter is returned to avoid exceptions during testing.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The transaction, or a null transaction adapter if the database is in-memory.</returns>
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (context.Database.IsInMemory())
            return new NullTransactionAdapter();

        var tx = await context.Database.BeginTransactionAsync(ct);
        return new TransactionAdapter(tx);
    }
    /// <summary>
    /// Retrieves a session for update by its ID. If the database is in-memory, it retrieves the session without locking. Otherwise, it uses a SQL query with "FOR UPDATE" to lock the row for update.
    /// </summary>
    /// <param name="sessionId">The ID of the session to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The session for update, or null if not found.</returns>
    public async Task<Session?> GetSessionForUpdateAsync(int sessionId, CancellationToken ct = default)
    {
        if (context.Database.IsInMemory())
            return await context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        return await context.Sessions
            .FromSqlInterpolated($"SELECT * FROM sessions WHERE id = {sessionId} FOR UPDATE")
            .FirstOrDefaultAsync(ct);
    }
    /// <summary>
    /// Counts the number of bookings for a given session ID.
    /// </summary>
    /// <param name="sessionId">The ID of the session for which to count bookings.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The number of bookings for the specified session.</returns>
    public Task<int> CountBookingsBySessionIdAsync(int sessionId, CancellationToken ct = default)
        => context.Bookings.CountAsync(b => b.SessionId == sessionId, ct);
    /// <summary>
    /// Checks if a user has a booking for a given session.
    /// </summary>
    /// <param name="sessionId">The ID of the session for which to check bookings.</param>
    /// <param name="userId">The ID of the user for whom to check bookings.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>true if the user has a booking for the specified session; otherwise, false.</returns>
    public Task<bool> HasUserBookingAsync(int sessionId, int userId, CancellationToken ct = default)
        => context.Bookings.AnyAsync(b => b.SessionId == sessionId && b.UserId == userId, ct);
    /// <summary>
    /// Checks if a user has an overlapping booking with a given time range. It retrieves the bookings for the user and checks if any of them overlap with the specified start and end times.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to check bookings.</param>
    /// <param name="start">The start time of the time range to check.</param>
    /// <param name="end">The end time of the time range to check.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>true if the user has an overlapping booking; otherwise, false.</returns>
    public Task<bool> HasOverlappingBookingAsync(int userId, DateTime start, DateTime end, CancellationToken ct = default)
        => context.Bookings
            .Include(b => b.Session)
            .Where(b => b.UserId == userId)
            .AnyAsync(b => b.Session!.StartsAt < end && start < b.Session.StartsAt.AddMinutes(b.Session.DurationMinutes), ct);
    /// <summary>
    /// Adds a new booking to the database context. The booking will be saved to the database when SaveChangesAsync is called.
    /// </summary>
    /// <param name="booking">The booking to add.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddBookingAsync(Booking booking, CancellationToken ct = default)
        => await context.Bookings.AddAsync(booking, ct);
    /// <summary>
    /// Retrieves a booking by its ID, including the associated session. If the booking is not found, it returns null.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The booking if found; otherwise, null.</returns>
    public Task<Booking?> GetBookingWithSessionAsync(int bookingId, CancellationToken ct = default)
        => context.Bookings.Include(b => b.Session).FirstOrDefaultAsync(b => b.Id == bookingId, ct);
    /// <summary>
    /// Deletes a booking from the database context. The booking will be removed from the database when SaveChangesAsync is called.
    /// </summary>
    /// <param name="booking">The booking to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteBookingAsync(Booking booking, CancellationToken ct = default)
    {
        context.Bookings.Remove(booking);
        return Task.CompletedTask;
    }
    /// <summary>
    /// Retrieves an idempotency record by its key. If the record is not found, it returns null.
    /// </summary>
    /// <param name="key">The key of the idempotency record to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The idempotency record if found; otherwise, null.</returns>
    public Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, CancellationToken ct = default)
        => context.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);
    /// <summary>
    /// Saves a new idempotency record to the database context. The record will be saved to the database when SaveChangesAsync is called.
    /// </summary>
    /// <param name="record">The idempotency record to save.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken ct = default)
        => await context.IdempotencyRecords.AddAsync(record, ct);
    /// <summary>
    /// Saves all changes made in the context to the database. This method should be called after adding, updating, or deleting entities to persist the changes.    
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
/// <summary>
/// A null implementation of the ITransaction interface that does nothing. This is used when the database is in-memory to avoid exceptions during testing.
/// </summary>
public class NullTransactionAdapter : ITransaction
{
    /// <summary>
    /// Commits the transaction. In this null implementation, it does nothing and returns a completed task.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns></returns>
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    /// <summary>
    /// Rolls back the transaction. In this null implementation, it does nothing and returns a completed task.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns></returns>
    public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
    /// <summary>
    /// Disposes the transaction. In this null implementation, it does nothing and returns a completed ValueTask.
    /// </summary>
    /// <returns></returns>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}