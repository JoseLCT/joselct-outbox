namespace Joselct.Outbox.Core.Policies;

public class ExponentialBackoffStrategy : IBackoffStrategy
{
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;

    public ExponentialBackoffStrategy(
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null
    )
    {
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(30);
        _maxDelay = maxDelay ?? TimeSpan.FromHours(6);
    }

    public TimeSpan GetDelay(int retryCount)
    {
        var seconds = _baseDelay.TotalSeconds * Math.Pow(2, retryCount - 1);
        var capped = Math.Min(seconds, _maxDelay.TotalSeconds);
        return TimeSpan.FromSeconds(capped);
    }
}