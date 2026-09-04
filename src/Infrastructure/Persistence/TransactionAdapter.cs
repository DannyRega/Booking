using Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence;
/// <summary>
/// An adapter class that wraps an IDbContextTransaction and implements the ITransaction interface.
/// </summary>
/// <param name="transaction">The database context transaction to wrap.</param>
public class TransactionAdapter(IDbContextTransaction transaction) : ITransaction
{
    /// <summary>
    /// Commits the transaction asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns></returns>
    public Task CommitAsync(CancellationToken ct = default) => transaction.CommitAsync(ct);
    /// <summary>
    /// Rolls back the transaction asynchronously.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns></returns>
    public Task RollbackAsync(CancellationToken ct = default) => transaction.RollbackAsync(ct);
    /// <summary>
    /// Disposes the transaction asynchronously.
    /// </summary>
    /// <returns></returns>
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
