using Joselct.Outbox.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Joselct.Outbox.EFCore.Persistence;

public class EfOutboxDatabase<TContext> : IOutboxDatabase
    where TContext : DbContext
{
    private readonly TContext _context;

    public EfOutboxDatabase(TContext context) => _context = context;

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}