using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class StudentSessionConfiguration : IEntityTypeConfiguration<StudentSession>
{
    public void Configure(EntityTypeBuilder<StudentSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.TenantId, s.StudentId, s.SessionDate });
        builder.Property(s => s.Status).HasConversion<int>();
        builder.Property(s => s.SessionDate).HasColumnType("date");
    }
}
