using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
/// <summary>
/// Represents a repository for managing session data in the application.
/// </summary>
/// <param name="context">The application's database context.</param>
public class SessionRepository(ApplicationDbContext context) : ISessionRepository
{
    /// <summary>
    /// Retrieves a paginated list of sessions based on the provided filters and pagination parameters.
    /// </summary>
    /// <param name="from">The start time of the time range to filter sessions.</param>
    /// <param name="to">The end time of the time range to filter sessions.</param>
    /// <param name="instructor">The instructor name to filter sessions.</param>
    /// <param name="onlyAvailable">A value indicating whether to include only available sessions.</param>
    /// <param name="cursor">The cursor for pagination.</param>
    /// <param name="limit">The maximum number of sessions to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<CursorPagedResult<SessionResponseDto>> GetSessionsAsync(
        DateTime? from,
        DateTime? to,
        string? instructor,
        bool? onlyAvailable,
        int? cursor,
        int limit = 20,
        CancellationToken ct = default)
    {
        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var query = context.Sessions.AsNoTracking().AsQueryable();

        // 1. Filtros opcionales con forzado explícito a DateTimeKind.Utc
        if (from.HasValue)
        {
            var utcFrom = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(s => s.StartsAt >= utcFrom);
        }

        if (to.HasValue)
        {
            var utcTo = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(s => s.StartsAt <= utcTo);
        }

        if (!string.IsNullOrWhiteSpace(instructor))
        {
            query = query.Where(s => EF.Functions.ILike(s.Instructor, $"%{instructor.Trim()}%"));
        }

        // 2. Paginación por cursor O(1)
        if (cursor.HasValue && cursor.Value > 0)
        {
            query = query.Where(s => s.Id > cursor.Value);
        }

        // 3. Filtro onlyAvailable: antes del Select para traducción limpia a SQL
        if (onlyAvailable == true)
        {
            query = query.Where(s => s.Capacity > context.Bookings.Count(b => b.SessionId == s.Id));
        }

        // 4. Proyección final al DTO
        var projection = query
            .OrderBy(s => s.Id)
            .Select(s => new SessionResponseDto(
                s.Id,
                s.Title,
                s.Instructor,
                s.StartsAt,
                s.DurationMinutes,
                s.Capacity,
                s.Capacity - context.Bookings.Count(b => b.SessionId == s.Id)
            ));

        // 5. Lectura de limit + 1 para HasNextPage sin COUNT(*) adicional
        var items = await projection
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasNextPage = items.Count > limit;
        if (hasNextPage)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = items.Count > 0 ? items[^1].Id : (int?)null;

        return new CursorPagedResult<SessionResponseDto>(items, nextCursor, hasNextPage);
    }
}