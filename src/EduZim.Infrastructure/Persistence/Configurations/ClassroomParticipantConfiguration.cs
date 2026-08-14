using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ClassroomParticipantConfiguration : IEntityTypeConfiguration<ClassroomParticipant>
{
    public void Configure(EntityTypeBuilder<ClassroomParticipant> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.ClassroomSessionId, p.UserId }).IsUnique();
    }
}
