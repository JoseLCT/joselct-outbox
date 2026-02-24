using Joselct.Outbox.EFCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Joselct.Outbox.EFCore.Extensions;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ConfigureOutbox(this ModelBuilder modelBuilder, string schema = "outbox")
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfig(schema));
        return modelBuilder;
    }
}