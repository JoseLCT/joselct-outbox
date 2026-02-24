# Joselct.Outbox.Core

Core abstractions and entities for the [Joselct.Outbox](https://github.com/joselct/joselct-outbox) library.

This package contains the contracts and domain model shared across all Joselct.Outbox packages. Reference it directly
only if you need to share outbox contracts across multiple projects without pulling in the full infrastructure
implementation.

## Installation

```bash
dotnet add package Joselct.Outbox.Core
```

## What's Included

### Entities

**`OutboxMessage`** — represents a pending event stored in the outbox table.

```csharp
// Create a message — automatically captures the current OpenTelemetry trace
var message = OutboxMessage.CreateWithCurrentTrace(new OrderCreatedEvent(order.Id));

// Create with explicit correlation id
var message = OutboxMessage.Create(new OrderCreatedEvent(order.Id), correlationId: "abc-123");
```

### Abstractions

**`IOutboxPublisher`** — used by application handlers to store events in the outbox.

```csharp
public interface IOutboxPublisher
{
    // Automatically captures the active OpenTelemetry trace
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class;

    // Explicit correlation id when no active trace is available
    Task PublishAsync<T>(T @event, string correlationId, CancellationToken ct = default) where T : class;
}
```

**`IOutboxDispatcher`** — used by the background processor to dispatch deserialized events.

```csharp
public interface IOutboxDispatcher
{
    Task DispatchAsync(object @event, Type eventType, CancellationToken ct = default);
}
```

**`IOutboxRepository`** — persistence contract for outbox messages.

```csharp
public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
}
```

### Backoff Strategies

**`IBackoffStrategy`** — defines the delay between retries.

```csharp
// Exponential — 30s, 1m, 4m, 8m, ... (capped at MaxDelay)
new ExponentialBackoffStrategy(baseDelay: TimeSpan.FromSeconds(30), maxDelay: TimeSpan.FromHours(6));

// Fixed — same delay between every retry
new FixedBackoffStrategy(delay: TimeSpan.FromSeconds(60));
```

## Publisher vs Dispatcher

These two interfaces serve different purposes:

|               | `IOutboxPublisher`            | `IOutboxDispatcher`          |
|---------------|-------------------------------|------------------------------|
| **Used by**   | Application handlers          | Background processor         |
| **Direction** | Write to outbox               | Read from outbox and deliver |
| **When**      | Inside a business transaction | Asynchronously, after commit |

```
Handler → IOutboxPublisher → outbox_messages table → IOutboxDispatcher → broker
```

## Distributed Tracing

`OutboxMessage` stores the OpenTelemetry trace context (`TraceId`, `SpanId`, `CorrelationId`) when created. The
background processor restores this context when dispatching, so the processing span appears as a child of the original
request in tools like Jaeger or Zipkin.

```
HTTP Request (TraceId: abc)
  └── CreateOrder handler (SpanId: 111)
        └── OutboxProcessor (SpanId: 222)  ← linked to the original request
```

## Related Packages

- [Joselct.Outbox.EFCore](https://www.nuget.org/packages/Joselct.Outbox.EFCore) — Entity Framework Core implementation
- [Joselct.Outbox.MediatR](https://www.nuget.org/packages/Joselct.Outbox.MediatR) — MediatR dispatcher adapter

## License

MIT