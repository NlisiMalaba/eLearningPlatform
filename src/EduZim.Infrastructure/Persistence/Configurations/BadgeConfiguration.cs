using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(b => b.TenantId);
        builder.Property(b => b.Type).HasConversion<int>();
    }
}
