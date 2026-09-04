using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces;
/// <summary>
/// Defines the contract for a booking repository, providing methods for managing bookings, sessions, and idempotency records in the system.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Begins a new transaction for the booking repository.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the transaction object.</returns>
    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
    /// <summary>
    /// Gets a session for update by its ID.
    /// </summary>
    /// <param name="sessionId">The ID of the session to retrieve.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the session object, or null if not found.</returns>
    Task<Session?> GetSessionForUpdateAsync(int sessionId, CancellationToken ct = default);
    /// <summary>
    /// Counts the number of bookings for a given session ID.
    /// </summary>
    /// <param name="sessionId">The ID of the session for which to count bookings.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of bookings.</returns>
    Task<int> CountBookingsBySessionIdAsync(int sessionId, CancellationToken ct = default);
    /// <summary>
    /// Checks if a user has a booking for a given session.
    /// </summary>
    /// <param name="sessionId">The ID of the session to check.</param>
    /// <param name="userId">The ID of the user to check.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the user has a booking for the session.</returns>
    Task<bool> HasUserBookingAsync(int sessionId, int userId, CancellationToken ct = default);
    /// <summary>
    /// Checks if a user has an overlapping booking with a given time range.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <param name="start">The start time of the time range to check.</param>
    /// <param name="end">The end time of the time range to check.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the user has an overlapping booking.</returns>
    Task<bool> HasOverlappingBookingAsync(int userId, DateTime start, DateTime end, CancellationToken ct = default);
    /// <summary>
    /// Adds a new booking to the repository.
    /// </summary>
    /// <param name="booking">The booking to add.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddBookingAsync(Booking booking, CancellationToken ct = default);
    /// <summary>
    /// Gets a booking along with its associated session by the booking ID.
    /// </summary>
    /// <param name="bookingId">The ID of the booking to retrieve.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the booking object, or null if not found.</returns>
    Task<Booking?> GetBookingWithSessionAsync(int bookingId, CancellationToken ct = default);
    /// <summary>
    /// Deletes a booking from the repository.
    /// </summary>
    /// <param name="booking">The booking to delete.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteBookingAsync(Booking booking, CancellationToken ct = default);
    /// <summary>
    /// Gets an idempotency record by its key.
    /// </summary>
    /// <param name="key">The key of the idempotency record to retrieve.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the idempotency record, or null if not found.</returns>
    Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, CancellationToken ct = default);
    /// <summary>
    /// Saves an idempotency record to the repository.
    /// </summary>
    /// <param name="record">The idempotency record to save.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken ct = default);
    /// <summary>
    /// Saves all changes made in the repository to the underlying data store.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of state entries written to the data store.</returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}