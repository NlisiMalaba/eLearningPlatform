using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class SyncConflictLogConfiguration : IEntityTypeConfiguration<SyncConflictLog>
{
    public void Configure(EntityTypeBuilder<SyncConflictLog> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.TenantId, s.StudentId, s.CreatedAt });
        builder.Property(s => s.ResourceType).HasMaxLength(64).IsRequired();
        builder.HasOne<OfflineSyncQueue>().WithMany().HasForeignKey(s => s.OfflineSyncQueueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
