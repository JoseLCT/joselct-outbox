namespace Joselct.Outbox.Core.Policies;

public class FixedBackoffStrategy : IBackoffStrategy
{
    private readonly TimeSpan _delay;

    public FixedBackoffStrategy(TimeSpan delay)
    {
        _delay = delay;
    }

    public TimeSpan GetDelay(int retryCount) => _delay;
}