using Microsoft.EntityFrameworkCore;
using Joselct.Outbox.Core.Entities;
using Joselct.Outbox.Core.Repositories;

namespace Joselct.Outbox.EFCore.Persistence;

public class OutboxRepository<TContext> : IOutboxRepository where TContext : DbContext
{
    private readonly TContext _dbContext;

    public OutboxRepository(TContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await _dbContext.Set<OutboxMessage>().AddAsync(message, ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken ct = default
    )
    {
        return await _dbContext.Set<OutboxMessage>()
            .Where(x => x.ProcessedAt == null && x.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        return _dbContext.SaveChangesAsync(ct);
    }
}