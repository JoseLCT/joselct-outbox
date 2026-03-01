namespace Joselct.Outbox.Core.Config;

public class OutboxOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int BatchSize { get; set; } = 20;
    public int MaxRetries { get; set; } = 5;
}