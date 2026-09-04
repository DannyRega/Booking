using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    /// <summary>
    /// Represents a repository for managing session data, including retrieving sessions based on various filters and pagination options.
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>
        /// Gets a paginated list of sessions based on the provided filters.
        /// </summary>
        /// <param name="from">The start date for the session range.</param>
        /// <param name="to">The end date for the session range.</param>
        /// <param name="instructor">The instructor for the sessions.</param>
        /// <param name="onlyAvailable">A value indicating whether to include only available sessions.</param>
        /// <param name="cursor">The cursor for the current page.</param>
        /// <param name="limit">The maximum number of items to return.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the paginated list of sessions.</returns>
        Task<CursorPagedResult<SessionResponseDto>> GetSessionsAsync(
            DateTime? from,
            DateTime? to,
            string? instructor,
            bool? onlyAvailable,
            int? cursor,
            int limit = 20,
            CancellationToken ct = default);
    }
}
