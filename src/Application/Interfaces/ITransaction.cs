using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    /// <summary>
    /// Represents a transaction that can be committed or rolled back asynchronously.
    /// </summary>
    public interface ITransaction : IAsyncDisposable
    {
        /// <summary>
        /// Commits the transaction asynchronously.
        /// </summary>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CommitAsync(CancellationToken ct = default);
        /// <summary>
        /// Rolls back the transaction asynchronously.
        /// </summary>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task RollbackAsync(CancellationToken ct = default);
    }
}
