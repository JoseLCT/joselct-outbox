using Joselct.Outbox.Core.Entities;
using Joselct.Outbox.Core.Publishing;
using Joselct.Outbox.Core.Repositories;

namespace Joselct.Outbox.EFCore.Persistence;

public class OutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxRepository _repository;

    public OutboxPublisher(IOutboxRepository repository)
    {
        _repository = repository;
    }

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
    {
        return _repository.AddAsync(OutboxMessage.CreateWithCurrentTrace(@event), ct);
    }

    public Task PublishAsync<T>(T @event, string correlationId, CancellationToken ct = default) where T : class
    {
        return _repository.AddAsync(OutboxMessage.Create(@event, correlationId), ct);
    }
}