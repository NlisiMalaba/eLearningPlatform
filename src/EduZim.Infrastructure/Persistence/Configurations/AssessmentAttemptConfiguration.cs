using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class AssessmentAttemptConfiguration : IEntityTypeConfiguration<AssessmentAttempt>
{
    public void Configure(EntityTypeBuilder<AssessmentAttempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(a => a.TenantId);
        builder.Property(a => a.TimedAutoSubmitHangfireJobId).HasMaxLength(128);
        builder.HasOne<Assessment>().WithMany().HasForeignKey(a => a.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AssessmentClassAssignment>().WithMany().HasForeignKey(a => a.AssessmentClassAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
