using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class SubjectProgressConfiguration : IEntityTypeConfiguration<SubjectProgress>
{
    public void Configure(EntityTypeBuilder<SubjectProgress> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.TenantId, s.StudentId, s.Subject }).IsUnique();
        builder.Property(s => s.Subject).HasMaxLength(128).IsRequired();
    }
}
