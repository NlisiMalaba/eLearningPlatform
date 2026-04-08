using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ModuleContentItemConfiguration : IEntityTypeConfiguration<ModuleContentItem>
{
    public void Configure(EntityTypeBuilder<ModuleContentItem> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(m => m.TenantId);
        builder.HasOne<Module>().WithMany(m => m.ContentItems).HasForeignKey(m => m.ModuleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(m => m.ContentItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
