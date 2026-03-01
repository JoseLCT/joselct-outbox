using System.Diagnostics;
using System.Text.Json;
using Joselct.Outbox.Core.Policies;

namespace Joselct.Outbox.Core.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? TraceId { get; private set; }
    public string? SpanId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create<T>(
        T content,
        string? correlationId = null,
        string? traceId = null,
        string? spanId = null
    ) where T : class
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(T).FullName ?? typeof(T).Name,
            Payload = JsonSerializer.Serialize(content),
            CreatedAt = DateTime.UtcNow,
            NextRetryAt = DateTime.UtcNow,
            CorrelationId = correlationId,
            TraceId = traceId,
            SpanId = spanId
        };
    }

    public static OutboxMessage CreateWithCurrentTrace<T>(T content) where T : class
    {
        var activity = Activity.Current;
        return Create(
            content,
            correlationId: activity?.GetBaggageItem("correlation_id"),
            traceId: activity?.TraceId.ToString(),
            spanId: activity?.SpanId.ToString()
        );
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        NextRetryAt = null;
    }

    public void RegisterFailure(int maxRetries, IBackoffStrategy strategy)
    {
        RetryCount++;

        if (RetryCount >= maxRetries)
        {
            NextRetryAt = null;
            return;
        }

        var delay = strategy.GetDelay(RetryCount);
        NextRetryAt = DateTime.UtcNow.Add(delay);
    }

    public bool IsProcessed => ProcessedAt.HasValue;
    public bool IsDeadLetter => !IsProcessed && NextRetryAt is null && RetryCount > 0;
    public bool IsReadyToProcess => NextRetryAt.HasValue && NextRetryAt <= DateTime.UtcNow;
}