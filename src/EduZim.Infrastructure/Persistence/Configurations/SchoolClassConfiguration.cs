using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(c => c.TenantId);
        builder.Property(c => c.Name).HasMaxLength(256);
    }
}
