using MediatR;
using Joselct.Outbox.Core.Dispatching;

namespace Joselct.Outbox.MediatR;

public class MediatROutboxDispatcher : IOutboxDispatcher
{
    private readonly IPublisher _publisher;

    public MediatROutboxDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DispatchAsync(object @event, Type eventType, CancellationToken ct = default)
    {
        if (@event is INotification notification)
        {
            await _publisher.Publish(notification, ct);
            return;
        }

        throw new InvalidOperationException(
            $"The event of type {eventType.FullName} does not implement INotification and cannot be dispatched."
        );
    }
}