using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(q => q.TenantId);
        builder.Property(q => q.Type).HasConversion<int>();
        builder.Property(q => q.Text).HasMaxLength(4000);
        builder.HasOne<Assessment>().WithMany(a => a.Questions).HasForeignKey(q => q.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(q => q.Options).WithOne().HasForeignKey(o => o.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}
