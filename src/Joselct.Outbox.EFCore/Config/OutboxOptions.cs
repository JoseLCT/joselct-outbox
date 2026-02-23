namespace Joselct.Outbox.EFCore.Config;

public class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; set; } = 20;
    public int IntervalSeconds { get; set; } = 10;
    public int MaxRetries { get; set; } = 5;

    public BackoffStrategyType BackoffStrategy { get; set; } = BackoffStrategyType.Exponential;
    public int BaseDelaySeconds { get; set; } = 30;
    public int MaxDelayHours { get; set; } = 6;

    public bool EnableCleanup { get; set; } = true;
    public int CleanupAfterDays { get; set; } = 7;
}