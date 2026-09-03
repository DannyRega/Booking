using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ITransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}
