using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.Role).HasConversion<int>();
        builder.Property(u => u.FontSize)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(FontSize.Medium);
        builder.Property(u => u.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(u => u.TenantId);
        builder.HasIndex(u => new { u.TenantId, u.Role, u.LastLoginAt });
    }
}
