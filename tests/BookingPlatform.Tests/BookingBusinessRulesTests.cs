using Application.Services;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BookingPlatform.Tests;
/// <summary>
/// Contains unit tests for the BookingService, focusing on business rules such as idempotency, session overlap, and capacity limits.
/// </summary>
public class BookingBusinessRulesTests
{
    /// <summary>
    /// Creates a new instance of BookingService and ApplicationDbContext for testing purposes, using an in-memory database.
    /// </summary>
    /// <param name="dbName">The name of the database to use for testing.</param>
    /// <returns>A tuple containing the BookingService and ApplicationDbContext instances.</returns>
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

    /// <summary>
    /// Tests that the BookingService correctly handles idempotent requests by returning the same response for repeated requests with the same idempotency key, without creating duplicate bookings.
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// Tests that the BookingService correctly handles overlapping sessions by returning a conflict response when a user attempts to book a session that overlaps with an existing booking.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task OverlappingSessions_PartialOverlap_ShouldReturnConflict()
    {
        var (service, db) = CreateSut("OverlapPartialDb");

        // 10:00 a 11:30
        var s1 = new Session { Id = 1, StartsAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc), DurationMinutes = 90, Capacity = 10 };
        // 10:30 a 11:30 (empieza mientras s1 sigue activa)
        var s2 = new Session { Id = 2, StartsAt = new DateTime(2026, 10, 1, 10, 30, 0, DateTimeKind.Utc), DurationMinutes = 60, Capacity = 10 };

        db.Sessions.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var (s1Status, _) = await service.CreateBookingAsync(1, 1, null, CancellationToken.None);
        var (s2Status, _) = await service.CreateBookingAsync(1, 2, null, CancellationToken.None);

        Assert.Equal(201, s1Status);
        Assert.Equal(409, s2Status);
    }
    /// <summary>
    /// Tests that the BookingService correctly handles completely enclosed sessions by returning a conflict response when a user attempts to book a session that is entirely contained within the time frame of an existing booking.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task OverlappingSessions_CompletelyEnclosedSession_ShouldReturnConflict()
    {
        var (service, db) = CreateSut("OverlapEnclosedDb");

        // Sesión corta previa reservada: 11:00 a 12:00 (60 min)
        var sShort = new Session { Id = 10, StartsAt = new DateTime(2026, 10, 1, 11, 0, 0, DateTimeKind.Utc), DurationMinutes = 60, Capacity = 10 };

        // Nueva sesión larga que envuelve a la corta: 10:00 a 14:00 (240 min)
        var sLong = new Session { Id = 20, StartsAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc), DurationMinutes = 240, Capacity = 10 };

        db.Sessions.AddRange(sShort, sLong);
        await db.SaveChangesAsync();

        // Usuario 5 reserva la sesión corta
        var (statusShort, _) = await service.CreateBookingAsync(5, 10, null, CancellationToken.None);
        Assert.Equal(201, statusShort);

        // El mismo usuario intenta reservar la sesión larga envolvente
        var (statusLong, _) = await service.CreateBookingAsync(5, 20, null, CancellationToken.None);
        Assert.Equal(409, statusLong);
    }

    /// <summary>
    /// Tests that the BookingService correctly enforces session capacity limits by rejecting further booking requests with a conflict response when the number of bookings exceeds the session's capacity.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CapacityLimit_WhenExceeded_RejectsFurtherBookingsWithConflict()
    {
        var (service, db) = CreateSut("CapacityDb");

        // Sesión con solo 2 lugares disponibles
        var session = new Session { Id = 99, StartsAt = DateTime.UtcNow.AddDays(2), DurationMinutes = 60, Capacity = 2 };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        // Disparamos 5 solicitudes paralelas de usuarios diferentes
        var tasks = Enumerable.Range(1, 5).Select(userId =>
            service.CreateBookingAsync(userId, 99, null, CancellationToken.None)
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.StatusCode == 201);
        var conflictCount = results.Count(r => r.StatusCode == 409);

        // Nunca deben existir más reservas que la capacidad
        Assert.Equal(2, successCount);
        Assert.Equal(3, conflictCount);
        Assert.Equal(2, await db.Bookings.CountAsync(b => b.SessionId == 99));
    }
}