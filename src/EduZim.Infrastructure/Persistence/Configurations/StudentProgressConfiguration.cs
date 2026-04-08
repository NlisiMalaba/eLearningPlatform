using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class StudentProgressConfiguration : IEntityTypeConfiguration<StudentProgress>
{
    public void Configure(EntityTypeBuilder<StudentProgress> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.StudentId, s.ModuleId }).IsUnique();
    }
}
