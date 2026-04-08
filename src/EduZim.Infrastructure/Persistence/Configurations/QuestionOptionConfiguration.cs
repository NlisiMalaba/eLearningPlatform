using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(o => o.TenantId);
        builder.Property(o => o.Text).HasMaxLength(2000);
    }
}
