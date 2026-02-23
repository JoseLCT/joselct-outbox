using System.Diagnostics;
using System.Text.Json;
using Joselct.Outbox.EFCore.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Joselct.Outbox.Core.Dispatching;
using Joselct.Outbox.Core.Entities;
using Joselct.Outbox.Core.Policies;
using Joselct.Outbox.Core.Repositories;

namespace Joselct.Outbox.EFCore.Processing;

public class OutboxProcessor
{
    private static readonly ActivitySource ActivitySource = new("Joselct.Outbox");

    private readonly IOutboxRepository _repository;
    private readonly IOutboxDispatcher _dispatcher;
    private readonly IBackoffStrategy _backoff;
    private readonly int _maxRetries;
    private readonly int _batchSize;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxRepository repository,
        IOutboxDispatcher dispatcher,
        IBackoffStrategy backoff,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessor> logger
    )
    {
        _repository = repository;
        _dispatcher = dispatcher;
        _backoff = backoff;
        _maxRetries = options.Value.MaxRetries;
        _batchSize = options.Value.BatchSize;
        _logger = logger;
    }

    public async Task ProcessAsync(CancellationToken ct = default)
    {
        var messages = await _repository.GetPendingAsync(_batchSize, ct);
        if (!messages.Any()) return;

        foreach (var message in messages)
        {
            using var activity = StartActivity(message);

            try
            {
                var eventType = Type.GetType(message.Type)
                                ?? throw new InvalidOperationException($"Type not found: {message.Type}");

                var @event = JsonSerializer.Deserialize(message.Payload, eventType)
                             ?? throw new InvalidOperationException($"Failed to deserialize: {message.Id}");

                await _dispatcher.DispatchAsync(@event, eventType, ct);

                message.MarkAsProcessed();
                _logger.LogInformation("Processed message {Id} of type {Type}", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                message.RegisterFailure(_maxRetries, _backoff);
                _logger.LogError(
                    ex,
                    "Failed to process message {Id}, retry {Retry}/{Max}",
                    message.Id,
                    message.RetryCount,
                    _maxRetries
                );
            }
        }

        await _repository.CommitAsync(ct);
    }

    private static Activity? StartActivity(OutboxMessage message)
    {
        var parentContext = TryRestoreActivityContext(
            message.TraceId,
            message.SpanId
        );

        var activity = ActivitySource.StartActivity(
            "Joselct.Outbox.Processing",
            ActivityKind.Consumer,
            parentContext
        );

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("outbox.message_id", message.Id);
        activity.SetTag("outbox.message_type", message.Type);
        activity.SetTag("outbox.retry_count", message.RetryCount);

        if (!string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            activity.AddBaggage("correlation_id", message.CorrelationId);
        }

        return activity;
    }

    private static ActivityContext TryRestoreActivityContext(string? traceId, string? spanId)
    {
        if (string.IsNullOrEmpty(traceId) || string.IsNullOrEmpty(spanId))
        {
            return default;
        }

        try
        {
            return new ActivityContext(
                ActivityTraceId.CreateFromString(traceId.AsSpan()),
                ActivitySpanId.CreateFromString(spanId.AsSpan()),
                ActivityTraceFlags.Recorded
            );
        }
        catch
        {
            return default;
        }
    }
}