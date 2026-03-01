using Joselct.Outbox.Core.Entities;

namespace Joselct.Outbox.Core.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    void Update(OutboxMessage message);
}