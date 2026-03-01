using MediatR;

namespace Joselct.Outbox.MediatR.Notifications;

public class OutboxMessageNotification<T> : INotification where T : class
{
    public Guid MessageId { get; }
    public T Content { get; }
    public string? CorrelationId { get; }
    public string? TraceId { get; }
    public string? SpanId { get; }

    public OutboxMessageNotification(
        Guid messageId,
        T content,
        string? correlationId,
        string? traceId,
        string? spanId)
    {
        MessageId = messageId;
        Content = content;
        CorrelationId = correlationId;
        TraceId = traceId;
        SpanId = spanId;
    }
}