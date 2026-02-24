# Joselct.Outbox.MediatR

MediatR dispatcher adapter for the [Joselct.Outbox](https://github.com/joselct/joselct-outbox) library.

This package connects the Outbox background processor to MediatR, so processed outbox messages are dispatched as `INotification` events to their corresponding `INotificationHandler` implementations.

## Installation

```bash
dotnet add package Joselct.Outbox.MediatR
```

## Requirements

- Joselct.Outbox.EFCore
- MediatR 14+

## Setup

### 1. Register services

```csharp
builder.Services
    .AddOutbox<AppDbContext>(builder.Configuration)
    .AddOutboxMediatR();
```

Make sure MediatR is also registered and your handlers are included in the assembly scan:

```csharp
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
```

### 2. Define your events

Your domain events must implement `INotification`:

```csharp
public record OrderCreatedEvent(Guid OrderId, string CustomerEmail) : INotification;
```

### 3. Implement notification handlers

```csharp
public class OrderCreatedHandler : INotificationHandler<OrderCreatedEvent>
{
    private readonly IEmailService _emailService;

    public OrderCreatedHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(OrderCreatedEvent notification, CancellationToken ct)
    {
        await _emailService.SendConfirmationAsync(notification.CustomerEmail, ct);
    }
}
```

### 4. Publish events from your handlers

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

        await _outbox.PublishAsync(new OrderCreatedEvent(order.Id, command.Email), ct);

        await _db.SaveChangesAsync(ct); // atomic — order + outbox message
    }
}
```

## How It Works

`MediatROutboxDispatcher` implements `IOutboxDispatcher` and bridges the outbox processor with MediatR:

```csharp
public class MediatROutboxDispatcher : IOutboxDispatcher
{
    public async Task DispatchAsync(object @event, Type eventType, CancellationToken ct)
    {
        if (@event is INotification notification)
            await _publisher.Publish(notification, ct);
        else
            throw new InvalidOperationException(
                $"{eventType.Name} must implement INotification to use the MediatR dispatcher.");
    }
}
```

The full flow looks like this:

```
CreateOrderHandler
  └── IOutboxPublisher.PublishAsync(new OrderCreatedEvent(...))
        └── serialized + saved to outbox_messages (same transaction)

OutboxBackgroundService (every N seconds)
  └── OutboxProcessor reads pending messages
        └── MediatROutboxDispatcher.DispatchAsync(event, eventType)
              └── IPublisher.Publish(notification)
                    └── OrderCreatedHandler.Handle(notification)
```

## Important: Events Must Implement INotification

If you publish an event that does not implement `INotification`, the dispatcher will throw an `InvalidOperationException` at dispatch time. Make sure all events you publish via `IOutboxPublisher` implement `INotification` when using this adapter.

```csharp
// ✅ Correct
public record OrderCreatedEvent(Guid OrderId) : INotification;

// ❌ Will throw at dispatch time
public record OrderCreatedEvent(Guid OrderId);
```

## Using a Different Dispatcher

If you prefer not to use MediatR, you can implement `IOutboxDispatcher` directly and skip this package entirely:

```csharp
public class MyCustomDispatcher : IOutboxDispatcher
{
    public async Task DispatchAsync(object @event, Type eventType, CancellationToken ct)
    {
        // publish to RabbitMQ
    }
}

builder.Services
    .AddOutbox<AppDbContext>(builder.Configuration)
    .AddScoped<IOutboxDispatcher, MyCustomDispatcher>();
```

## Related Packages

- [Joselct.Outbox.Core](https://www.nuget.org/packages/Joselct.Outbox.Core) — Core abstractions
- [Joselct.Outbox.EFCore](https://www.nuget.org/packages/Joselct.Outbox.EFCore) — Entity Framework Core implementation

## License

MIT