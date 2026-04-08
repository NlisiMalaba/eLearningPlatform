using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class AnswerRecordConfiguration : IEntityTypeConfiguration<AnswerRecord>
{
    public void Configure(EntityTypeBuilder<AnswerRecord> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(a => a.TenantId);
        builder.HasOne<AssessmentAttempt>().WithMany(a => a.Answers).HasForeignKey(a => a.AssessmentAttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Question>().WithMany().HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}
