namespace Joselct.Outbox.Core.Dispatching;

public interface IOutboxDispatcher
{
    Task DispatchAsync(object @event, Type eventType, CancellationToken ct = default);
}