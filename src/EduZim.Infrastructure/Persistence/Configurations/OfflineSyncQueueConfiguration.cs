using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class OfflineSyncQueueConfiguration : IEntityTypeConfiguration<OfflineSyncQueue>
{
    public void Configure(EntityTypeBuilder<OfflineSyncQueue> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(o => o.TenantId);
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.Payload).HasMaxLength(100_000);
    }
}
