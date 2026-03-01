namespace Joselct.Outbox.Core.Interfaces;

public interface IOutboxDatabase
{
    Task CommitAsync(CancellationToken ct = default);
}