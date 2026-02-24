# Joselct.Outbox

A lightweight, extensible implementation of the **Outbox Pattern** for .NET 10, designed to ensure reliable message delivery in distributed systems and microservices.

## What is the Outbox Pattern?

The Outbox Pattern guarantees that domain events are published reliably by storing them in the same database transaction as your business data. A background processor then picks up those events and dispatches them — ensuring no message is lost even if the broker is temporarily unavailable.

```
Handler
  └── BEGIN TRANSACTION
        ├── INSERT orders
        └── INSERT outbox_messages   ← same transaction
      COMMIT

OutboxProcessor (background)
  └── reads outbox_messages
  └── dispatches to broker / MediatR
  └── marks as processed
```

## Packages

| Package | Description | NuGet |
|---|---|---|
| `Joselct.Outbox.Core` | Core abstractions and entities | [![NuGet](https://img.shields.io/nuget/v/Joselct.Outbox.Core)](https://www.nuget.org/packages/Joselct.Outbox.Core) |
| `Joselct.Outbox.EFCore` | Entity Framework Core implementation | [![NuGet](https://img.shields.io/nuget/v/Joselct.Outbox.EFCore)](https://www.nuget.org/packages/Joselct.Outbox.EFCore) |
| `Joselct.Outbox.MediatR` | MediatR dispatcher adapter | [![NuGet](https://img.shields.io/nuget/v/Joselct.Outbox.MediatR)](https://www.nuget.org/packages/Joselct.Outbox.MediatR) |

## Quick Start

### 1. Install packages

```bash
dotnet add package Joselct.Outbox.EFCore
dotnet add package Joselct.Outbox.MediatR  # optional — only if using MediatR
```

### 2. Configure your DbContext

```csharp
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureOutbox(); // adds outbox_messages table
    }
}
```

### 3. Register services

```csharp
// With MediatR
builder.Services
    .AddOutbox<AppDbContext>(builder.Configuration)
    .AddOutboxMediatR();

// With a custom dispatcher
builder.Services
    .AddOutbox<AppDbContext>(builder.Configuration)
    .AddScoped<IOutboxDispatcher, MyCustomDispatcher>();
```

### 4. Configure appsettings.json

```json
{
  "Outbox": {
    "BatchSize": 20,
    "IntervalSeconds": 10,
    "MaxRetries": 5,
    "BackoffStrategy": "Exponential",
    "BaseDelaySeconds": 30,
    "MaxDelayHours": 6,
    "EnableCleanup": true,
    "CleanupAfterDays": 7
  }
}
```

### 5. Publish events from your handlers

```csharp
public class CreateOrderHandler
{
    private readonly AppDbContext _db;
    private readonly IOutboxPublisher _outbox;

    public CreateOrderHandler(AppDbContext db, IOutboxPublisher outbox)
    {
        _db = db;
        _outbox = outbox;
    }

    public async Task HandleAsync(CreateOrderCommand command, CancellationToken ct)
    {
        var order = new Order { ... };
        _db.Orders.Add(order);

        // Stored in the same transaction as the order
        await _outbox.PublishAsync(new OrderCreatedEvent(order.Id), ct);

        await _db.SaveChangesAsync(ct); // atomic — order + outbox message
    }
}
```

### 6. Handle events with MediatR

```csharp
public class OrderCreatedEvent : INotification
{
    public Guid OrderId { get; init; }
}

public class OrderCreatedHandler : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken ct)
    {
        // process the event
    }
}
```

## Features

- **Guaranteed delivery** — events are stored atomically with your business data
- **Automatic retries** — configurable exponential or fixed backoff strategies
- **Distributed tracing** — OpenTelemetry support with trace context propagation
- **Automatic cleanup** — configurable cleanup of processed messages
- **Dispatcher agnostic** — works with MediatR, RabbitMQ, Kafka, or any custom dispatcher

## Backoff Strategies

```json
// Exponential backoff (default) — 30s, 1m, 4m, 8m, ...
{ "BackoffStrategy": "Exponential", "BaseDelaySeconds": 30, "MaxDelayHours": 6 }

// Fixed backoff — same delay between every retry
{ "BackoffStrategy": "Fixed", "BaseDelaySeconds": 60 }
```

## License

MIT
