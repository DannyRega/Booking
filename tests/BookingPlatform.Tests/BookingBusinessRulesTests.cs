using Application.Services;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookingPlatform.Tests;

public class BookingBusinessRulesTests
{
    private (BookingService service, ApplicationDbContext db) CreateSut(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var db = new ApplicationDbContext(options);
        var repo = new BookingRepository(db);
        var service = new BookingService(repo);
        return (service, db);
    }

    [Fact]
    public async Task OverlappingSessions_ShouldReturnConflict()
    {
        var (service, db) = CreateSut("OverlapDb");

        var s1 = new Session { Id = 1, StartsAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc), DurationMinutes = 90, Capacity = 10 };
        var s2 = new Session { Id = 2, StartsAt = new DateTime(2026, 10, 1, 10, 30, 0, DateTimeKind.Utc), DurationMinutes = 30, Capacity = 10 };

        db.Sessions.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var (s1Status, _) = await service.CreateBookingAsync(1, 1, null, CancellationToken.None);
        Assert.Equal(201, s1Status);

        var (s2Status, _) = await service.CreateBookingAsync(1, 2, null, CancellationToken.None);
        Assert.Equal(409, s2Status);
    }

    [Fact]
    public async Task Idempotency_SameKeyReturnsCachedResponseWithoutDuplicating()
    {
        var (service, db) = CreateSut("IdempotencyDb");

        var s1 = new Session { Id = 1, StartsAt = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Capacity = 10 };
        db.Sessions.Add(s1);
        await db.SaveChangesAsync();

        var key = "idempotency-key-test-123";

        var (status1, _) = await service.CreateBookingAsync(1, 1, key, CancellationToken.None);
        var (status2, _) = await service.CreateBookingAsync(1, 1, key, CancellationToken.None);

        Assert.Equal(201, status1);
        Assert.Equal(201, status2);
        Assert.Equal(1, await db.Bookings.CountAsync());
    }
}