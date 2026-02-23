using Joselct.Outbox.EFCore.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Joselct.Outbox.Core.Entities;

namespace Joselct.Outbox.EFCore.Processing;

public class OutboxCleanupService<TContext> : BackgroundService
    where TContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;

    public OutboxCleanupService(IServiceScopeFactory scopeFactory, IOptions<OutboxOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableCleanup) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            var threshold = DateTime.UtcNow.AddDays(-_options.CleanupAfterDays);
            await context.Set<OutboxMessage>()
                .Where(x => x.ProcessedAt != null && x.ProcessedAt < threshold)
                .ExecuteDeleteAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}