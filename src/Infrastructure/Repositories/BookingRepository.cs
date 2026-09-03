using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository(ApplicationDbContext context) : IBookingRepository
{
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (context.Database.IsInMemory())
            return new NullTransactionAdapter();

        var tx = await context.Database.BeginTransactionAsync(ct);
        return new TransactionAdapter(tx);
    }

    public async Task<Session?> GetSessionForUpdateAsync(int sessionId, CancellationToken ct = default)
    {
        if (context.Database.IsInMemory())
            return await context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        return await context.Sessions
            .FromSqlInterpolated($"SELECT * FROM sessions WHERE id = {sessionId} FOR UPDATE")
            .FirstOrDefaultAsync(ct);
    }

    public Task<int> CountBookingsBySessionIdAsync(int sessionId, CancellationToken ct = default)
        => context.Bookings.CountAsync(b => b.SessionId == sessionId, ct);

    public Task<bool> HasUserBookingAsync(int sessionId, int userId, CancellationToken ct = default)
        => context.Bookings.AnyAsync(b => b.SessionId == sessionId && b.UserId == userId, ct);

    public Task<bool> HasOverlappingBookingAsync(int userId, DateTime start, DateTime end, CancellationToken ct = default)
        => context.Bookings
            .Include(b => b.Session)
            .Where(b => b.UserId == userId)
            .AnyAsync(b => b.Session!.StartsAt < end && start < b.Session.StartsAt.AddMinutes(b.Session.DurationMinutes), ct);

    public async Task AddBookingAsync(Booking booking, CancellationToken ct = default)
        => await context.Bookings.AddAsync(booking, ct);

    public Task<Booking?> GetBookingWithSessionAsync(int bookingId, CancellationToken ct = default)
        => context.Bookings.Include(b => b.Session).FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    public Task DeleteBookingAsync(Booking booking, CancellationToken ct = default)
    {
        context.Bookings.Remove(booking);
        return Task.CompletedTask;
    }

    public Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, CancellationToken ct = default)
        => context.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);

    public async Task SaveIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken ct = default)
        => await context.IdempotencyRecords.AddAsync(record, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}

// Adaptador Nulo para evitar excepciones en xUnit con EF Core InMemory
public class NullTransactionAdapter : ITransaction
{
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}