using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ContentPackRatingConfiguration : IEntityTypeConfiguration<ContentPackRating>
{
    public void Configure(EntityTypeBuilder<ContentPackRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => new { r.ContentPackId, r.UserId }).IsUnique();
        builder.Property(r => r.Review).HasMaxLength(2000);
    }
}
