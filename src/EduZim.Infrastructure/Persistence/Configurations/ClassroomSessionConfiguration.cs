using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ClassroomSessionConfiguration : IEntityTypeConfiguration<ClassroomSession>
{
    public void Configure(EntityTypeBuilder<ClassroomSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.TenantId, s.SchoolClassId, s.StartAtUtc });
        builder.Property(s => s.RoomId).HasMaxLength(128).IsRequired();
        builder.Property(s => s.RecordingUrl).HasMaxLength(2048);
    }
}
