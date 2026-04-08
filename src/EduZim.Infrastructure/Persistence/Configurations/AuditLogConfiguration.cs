using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(a => a.TenantId);
        builder.Property(a => a.Action).HasMaxLength(256);
        builder.Property(a => a.ResourceType).HasMaxLength(128);
    }
}
