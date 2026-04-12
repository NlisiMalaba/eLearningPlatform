using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class ClassEnrollmentConfiguration : IEntityTypeConfiguration<ClassEnrollment>
{
    public void Configure(EntityTypeBuilder<ClassEnrollment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.SchoolClassId, e.StudentUserId }).IsUnique();
        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(e => e.SchoolClassId).OnDelete(DeleteBehavior.Cascade);
    }
}
