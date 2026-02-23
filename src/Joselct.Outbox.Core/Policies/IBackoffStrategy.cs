namespace Joselct.Outbox.Core.Policies;

public interface IBackoffStrategy
{
    TimeSpan GetDelay(int retryCount);
}