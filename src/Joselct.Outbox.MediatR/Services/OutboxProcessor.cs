using System.Text.Json;
using Joselct.Outbox.Core.Config;
using Joselct.Outbox.Core.Entities;
using Joselct.Outbox.Core.Interfaces;
using Joselct.Outbox.Core.Policies;
using Joselct.Outbox.MediatR.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Joselct.Outbox.MediatR.Services;

public class OutboxProcessor
{
    private readonly IOutboxRepository _repository;
    private readonly IOutboxDatabase _database;
    private readonly IMediator _mediator;
    private readonly OutboxOptions _options;
    private readonly IBackoffStrategy _backoffStrategy;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxRepository repository,
        IOutboxDatabase database,
        IMediator mediator,
        IOptions<OutboxOptions> options,
        IBackoffStrategy backoffStrategy,
        ILogger<OutboxProcessor> logger
    )
    {
        _repository = repository;
        _database = database;
        _mediator = mediator;
        _options = options.Value;
        _backoffStrategy = backoffStrategy;
        _logger = logger;
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var messages = await _repository.GetPendingAsync(
            _options.BatchSize,
            cancellationToken);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(a => a.GetTypes())
                           .FirstOrDefault(t => t.FullName == message.Type)
                       ?? throw new InvalidOperationException(
                           $"Cannot resolve type '{message.Type}'.");

            var content = JsonSerializer.Deserialize(message.Payload, type)
                          ?? throw new InvalidOperationException(
                              $"Failed to deserialize payload for message '{message.Id}'.");

            var notificationType = typeof(OutboxMessageNotification<>).MakeGenericType(type);
            var notification = Activator.CreateInstance(
                                   notificationType,
                                   message.Id,
                                   content,
                                   message.CorrelationId,
                                   message.TraceId,
                                   message.SpanId) as INotification
                               ?? throw new InvalidOperationException(
                                   $"Failed to create notification for type '{message.Type}'.");

            await _mediator.Publish(notification, cancellationToken);

            message.MarkAsProcessed();
            _repository.Update(message);
            await _database.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Outbox message {MessageId} of type {Type} processed successfully.",
                message.Id, message.Type
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process outbox message {MessageId} of type {Type}. Retry {RetryCount}/{MaxRetries}.",
                message.Id, message.Type, message.RetryCount + 1, _options.MaxRetries
            );

            message.RegisterFailure(_options.MaxRetries, _backoffStrategy);
            _repository.Update(message);
            await _database.CommitAsync(cancellationToken);
        }
    }
}