using Joselct.Outbox.Core.Entities;

namespace Joselct.Outbox.Core.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
}