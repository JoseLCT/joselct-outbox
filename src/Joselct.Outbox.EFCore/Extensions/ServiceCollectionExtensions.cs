using Joselct.Outbox.Core.Interfaces;
using Joselct.Outbox.EFCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Joselct.Outbox.EFCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxEfCore<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IOutboxRepository, EfCoreOutboxRepository<TContext>>();
        services.AddScoped<IOutboxDatabase, EfOutboxDatabase<TContext>>();
        return services;
    }
}