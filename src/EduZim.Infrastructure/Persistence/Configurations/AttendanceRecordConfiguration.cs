using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.TenantId, a.ClassroomSessionId, a.StudentUserId }).IsUnique();
    }
}
