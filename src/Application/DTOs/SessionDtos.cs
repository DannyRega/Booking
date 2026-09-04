using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    /// <summary>
    /// Represents a response containing information about a session.
    /// </summary>
    /// <param name="Id">The ID of the session.</param>
    /// <param name="Title">The title of the session.</param>
    /// <param name="Instructor">The instructor for the session.</param>
    /// <param name="StartsAt">The date and time when the session starts.</param>
    /// <param name="DurationMinutes">The duration of the session in minutes.</param>
    /// <param name="Capacity">The maximum number of attendees for the session.</param>
    /// <param name="AvailableSeats">The number of available seats for the session.</param>
    public record SessionResponseDto(
        int Id,
        string Title,
        string Instructor,
        DateTime StartsAt,
        int DurationMinutes,
        int Capacity,
        int AvailableSeats
    );
    /// <summary>
    /// Represents a paginated result set using cursor-based pagination.
    /// </summary>
    /// <typeparam name="T">The type of items in the result set.</typeparam>
    /// <param name="Items">The list of items in the current page.</param>
    /// <param name="NextCursor">The cursor for the next page, or null if there is no next page.</param>
    /// <param name="HasNextPage">A value indicating whether there is a next page available.</param>
    public record CursorPagedResult<T>(
        IReadOnlyList<T> Items,
        int? NextCursor,
        bool HasNextPage
    );
}
