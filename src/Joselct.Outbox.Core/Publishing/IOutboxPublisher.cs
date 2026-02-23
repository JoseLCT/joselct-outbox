namespace Joselct.Outbox.Core.Publishing;

public interface IOutboxPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class;
    Task PublishAsync<T>(T @event, string correlationId, CancellationToken ct = default) where T : class;
}