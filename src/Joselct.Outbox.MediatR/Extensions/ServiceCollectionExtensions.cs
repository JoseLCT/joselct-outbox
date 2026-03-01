using Joselct.Outbox.Core.Config;
using Joselct.Outbox.MediatR.Services;
using Joselct.Outbox.MediatR.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Joselct.Outbox.MediatR.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxWorker(
        this IServiceCollection services,
        Action<OutboxBackoffOptions>? configure = null)
    {
        services.AddOptions<OutboxOptions>()
            .BindConfiguration("Outbox");

        var backoffOptions = new OutboxBackoffOptions();
        configure?.Invoke(backoffOptions);
        services.AddSingleton(backoffOptions.BackoffStrategy);

        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxWorker>();

        return services;
    }
}