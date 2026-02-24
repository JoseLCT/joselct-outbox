using Joselct.Outbox.EFCore.Persistence;
using Joselct.Outbox.EFCore.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Joselct.Outbox.Core.Policies;
using Joselct.Outbox.Core.Publishing;
using Joselct.Outbox.Core.Repositories;
using Joselct.Outbox.EFCore.Config;

namespace Joselct.Outbox.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox<TContext>(
        this IServiceCollection services,
        IConfiguration configuration
    ) where TContext : DbContext
    {
        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        services.AddScoped<IOutboxRepository, OutboxRepository<TContext>>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();

        services.AddSingleton<IBackoffStrategy>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OutboxOptions>>().Value;

            return options.BackoffStrategy switch
            {
                BackoffStrategyType.Fixed =>
                    new FixedBackoffStrategy(
                        TimeSpan.FromSeconds(options.BaseDelaySeconds)
                    ),

                _ =>
                    new ExponentialBackoffStrategy(
                        TimeSpan.FromSeconds(options.BaseDelaySeconds),
                        TimeSpan.FromHours(options.MaxDelayHours)
                    )
            };
        });

        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxBackgroundService>();
        services.AddHostedService<OutboxCleanupService<TContext>>();

        return services;
    }
}