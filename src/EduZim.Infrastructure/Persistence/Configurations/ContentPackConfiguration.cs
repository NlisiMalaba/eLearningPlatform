using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ContentPackConfiguration : IEntityTypeConfiguration<ContentPack>
{
    public void Configure(EntityTypeBuilder<ContentPack> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.Status);
        builder.Property(p => p.Title).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(4000).IsRequired();
        builder.Property(p => p.SchoolName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.TeacherName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.ContentPackId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.AccessRequests).WithOne().HasForeignKey(r => r.ContentPackId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Ratings).WithOne().HasForeignKey(r => r.ContentPackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
