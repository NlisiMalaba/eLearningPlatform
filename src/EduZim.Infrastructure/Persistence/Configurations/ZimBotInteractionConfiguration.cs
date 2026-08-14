using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ZimBotInteractionConfiguration : IEntityTypeConfiguration<ZimBotInteraction>
{
    public void Configure(EntityTypeBuilder<ZimBotInteraction> builder)
    {
        builder.HasKey(z => z.Id);
        builder.Property(z => z.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(z => z.TenantId);
        builder.HasIndex(z => new { z.TenantId, z.StudentId, z.CreatedAt });
        builder.Property(z => z.Question).HasMaxLength(4000);
        builder.Property(z => z.Response).HasMaxLength(8000);
        builder.Property(z => z.Language).HasMaxLength(32);
    }
}
