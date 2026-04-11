using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class TranscriptConfiguration : IEntityTypeConfiguration<Transcript>
{
    public void Configure(EntityTypeBuilder<Transcript> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => new { t.TenantId, t.ContentItemId }).IsUnique();
        builder.Property(t => t.StorageKey).HasMaxLength(1024);
        builder.HasOne<ContentItem>()
            .WithOne(c => c.Transcript)
            .HasForeignKey<Transcript>(t => t.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
