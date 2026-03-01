using Joselct.Outbox.Core.Entities;
using Joselct.Outbox.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Joselct.Outbox.EFCore.Persistence;

public class EfCoreOutboxRepository<TContext> : IOutboxRepository where TContext : DbContext
{
    private readonly TContext _dbContext;

    public EfCoreOutboxRepository(TContext dbContext)
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

    public void Update(OutboxMessage message)
    {
        _dbContext.Set<OutboxMessage>().Update(message);
    }
}