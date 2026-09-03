using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public record SessionResponseDto(
        int Id,
        string Title,
        string Instructor,
        DateTime StartsAt,
        int DurationMinutes,
        int Capacity,
        int AvailableSeats
    );

    public record CursorPagedResult<T>(
        IReadOnlyList<T> Items,
        int? NextCursor,
        bool HasNextPage
    );
}
