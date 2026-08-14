using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ContentPackItemConfiguration : IEntityTypeConfiguration<ContentPackItem>
{
    public void Configure(EntityTypeBuilder<ContentPackItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => new { i.ContentPackId, i.ContentItemId }).IsUnique();
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(i => i.ContentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
