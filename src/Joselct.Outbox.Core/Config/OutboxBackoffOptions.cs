using Joselct.Outbox.Core.Policies;

namespace Joselct.Outbox.Core.Config;

public class OutboxBackoffOptions
{
    public IBackoffStrategy BackoffStrategy { get; private set; } = new ExponentialBackoffStrategy();

    public OutboxBackoffOptions WithBackoffStrategy(IBackoffStrategy strategy)
    {
        BackoffStrategy = strategy;
        return this;
    }
}