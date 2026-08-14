using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ContentPackAccessRequestConfiguration : IEntityTypeConfiguration<ContentPackAccessRequest>
{
    public void Configure(EntityTypeBuilder<ContentPackAccessRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.RequestingTenantId);
        builder.HasIndex(r => new { r.ContentPackId, r.RequestingTenantId }).IsUnique();
        builder.Property(r => r.Status).HasConversion<int>();
    }
}
