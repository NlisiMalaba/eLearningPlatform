using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class CaptionTrackConfiguration : IEntityTypeConfiguration<CaptionTrack>
{
    public void Configure(EntityTypeBuilder<CaptionTrack> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.ContentItemId });
        builder.Property(c => c.Language).HasMaxLength(16);
        builder.Property(c => c.StorageKey).HasMaxLength(1024);
        builder.HasOne<ContentItem>()
            .WithMany(c => c.CaptionTracks)
            .HasForeignKey(c => c.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
