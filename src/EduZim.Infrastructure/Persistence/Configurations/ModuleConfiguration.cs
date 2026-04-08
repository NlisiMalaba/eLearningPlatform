using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(m => m.TenantId);
        builder.Property(m => m.Title).HasMaxLength(256);
        builder.Property(m => m.Subject).HasMaxLength(128);
        builder.Property(m => m.Grade).HasConversion<int>();
    }
}
