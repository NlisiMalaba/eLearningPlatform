using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(256);
        builder.OwnsOne(t => t.Branding, b =>
        {
            b.Property(x => x.SchoolName).HasMaxLength(256);
            b.Property(x => x.PrimaryColour).HasMaxLength(32);
            b.Property(x => x.LogoUrl).HasMaxLength(1024);
        });
    }
}
