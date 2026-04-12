using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class AssessmentClassAssignmentConfiguration : IEntityTypeConfiguration<AssessmentClassAssignment>
{
    public void Configure(EntityTypeBuilder<AssessmentClassAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.AssessmentId, a.SchoolClassId }).IsUnique();
        builder.HasOne<Assessment>().WithMany().HasForeignKey(a => a.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(a => a.SchoolClassId).OnDelete(DeleteBehavior.Restrict);
    }
}
