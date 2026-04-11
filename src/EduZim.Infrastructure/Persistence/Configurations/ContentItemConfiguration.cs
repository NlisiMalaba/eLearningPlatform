using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(c => c.TenantId);
        builder.Property(c => c.Title).HasMaxLength(512);
        builder.Property(c => c.Type).HasConversion<int>();
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.StorageKey).HasMaxLength(1024);
        builder.Property(c => c.PermanentDeletionHangfireJobId).HasMaxLength(128);
    }
}
