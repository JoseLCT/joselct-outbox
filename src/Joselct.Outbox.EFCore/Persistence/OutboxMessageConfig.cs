using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Joselct.Outbox.Core.Entities;

namespace Joselct.Outbox.EFCore.Persistence;

internal class OutboxMessageConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Type).HasColumnName("type").IsRequired().HasMaxLength(500);
        builder.Property(x => x.Payload).HasColumnName("payload").IsRequired().HasColumnType("text");
        builder.Property(x => x.RetryCount).HasColumnName("retry_count");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(x => x.TraceId).HasColumnName("trace_id").HasMaxLength(100);
        builder.Property(x => x.SpanId).HasColumnName("span_id").HasMaxLength(100);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.NextRetryAt).HasColumnName("next_retry_at");

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ProcessedAt);
        builder.HasIndex(x => x.NextRetryAt);
    }
}