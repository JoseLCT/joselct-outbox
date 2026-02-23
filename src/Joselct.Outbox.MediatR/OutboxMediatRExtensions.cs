using Microsoft.Extensions.DependencyInjection;
using Joselct.Outbox.Core.Dispatching;

namespace Joselct.Outbox.MediatR;

public static class OutboxMediatRExtensions
{
    public static IServiceCollection AddOutboxMediatR(this IServiceCollection services)
    {
        services.AddScoped<IOutboxDispatcher, MediatROutboxDispatcher>();
        return services;
    }
}