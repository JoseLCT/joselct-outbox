# Joselct.Outbox.EFCore

Entity Framework Core implementation for the [Joselct.Outbox](https://github.com/joselct/joselct-outbox) library.

## Installation

```bash
dotnet add package Joselct.Outbox.EFCore
```

## Requirements

- .NET 10
- Entity Framework Core 10+
- A relational database (PostgreSQL, SQL Server, MySQL, etc.)

## Setup

### 1. Configure your DbContext

Call `ConfigureOutbox()` in `OnModelCreating` to register the outbox tables:

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureOutbox(); // registers outbox_messages table
    }
}
```

### 2. Add and run migrations

```bash
dotnet ef migrations add AddOutbox
dotnet ef database update
```

This creates the following table:

| Table                    | Description                  |
|--------------------------|------------------------------|
| `outbox.outbox_messages` | Pending and processed events |

### 3. Register services

```csharp
builder.Services.AddOutbox<AppDbContext>(builder.Configuration);
```

You must also register an `IOutboxDispatcher` implementation. Use `Joselct.Outbox.MediatR` or provide your own:

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

| Option             | Default       | Description                                         |
|--------------------|---------------|-----------------------------------------------------|
| `BatchSize`        | `20`          | Messages processed per cycle                        |
| `IntervalSeconds`  | `10`          | Seconds between processor cycles                    |
| `MaxRetries`       | `5`           | Max dispatch attempts before the message is skipped |
| `BackoffStrategy`  | `Exponential` | `Exponential` or `Fixed`                            |
| `BaseDelaySeconds` | `30`          | Base delay for backoff calculation                  |
| `MaxDelayHours`    | `6`           | Maximum delay cap for exponential backoff           |
| `EnableCleanup`    | `true`        | Auto-delete processed messages                      |
| `CleanupAfterDays` | `7`           | Days to retain processed messages                   |

## Usage

### Publishing events

Inject `IOutboxPublisher` in your handlers and call `PublishAsync` before `SaveChangesAsync`:

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
        var order = new Order { Id = Guid.NewGuid(), ... };
        _db.Orders.Add(order);

        // Stored in the same transaction as the order — atomic
        await _outbox.PublishAsync(new OrderCreatedEvent(order.Id), ct);

        await _db.SaveChangesAsync(ct);
    }
}
```

## How It Works

### Background processing

`OutboxBackgroundService` runs a loop every `IntervalSeconds` seconds. On each cycle it creates a new DI scope and calls
`OutboxProcessor.ProcessAsync()`:

```
Every N seconds:
  1. SELECT pending messages WHERE next_retry_at <= NOW
  2. For each message:
     a. Deserialize payload
     b. Restore OpenTelemetry trace context
     c. Dispatch via IOutboxDispatcher
     d. Success → MarkAsProcessed()
     e. Failure → RegisterFailure() with backoff delay
  3. SaveChanges (single commit per batch)
```

### Retry backoff

Failed messages are retried with increasing delays to avoid hammering an unavailable dependency:

```
Retry 1 → 30 seconds
Retry 2 → 1 minute
Retry 3 → 4 minutes
Retry 4 → 8 minutes
Retry 5 → message is skipped (NextRetryAt = null)
```

### Automatic cleanup

`OutboxCleanupService` runs daily and deletes processed messages older than `CleanupAfterDays` using
`ExecuteDeleteAsync` — without loading entities into memory.

## Custom Dispatcher

Implement `IOutboxDispatcher` to dispatch events to any broker:

```csharp
public class MyCustomDispatcher : IOutboxDispatcher
{
    public async Task DispatchAsync(object @event, Type eventType, CancellationToken ct)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(@event);
        // publish to RabbitMQ exchange
    }
}

// Register
builder.Services
    .AddOutbox<AppDbContext>(builder.Configuration)
    .AddScoped<IOutboxDispatcher, MyCustomDispatcher>();
```

## Related Packages

- [Joselct.Outbox.Core](https://www.nuget.org/packages/Joselct.Outbox.Core) — Core abstractions
- [Joselct.Outbox.MediatR](https://www.nuget.org/packages/Joselct.Outbox.MediatR) — MediatR dispatcher adapter

## License

MIT