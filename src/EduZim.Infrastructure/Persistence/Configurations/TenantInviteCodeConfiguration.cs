using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class TenantInviteCodeConfiguration : IEntityTypeConfiguration<TenantInviteCode>
{
    public void Configure(EntityTypeBuilder<TenantInviteCode> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(e => e.TenantId);
        builder.Property(e => e.Code).HasMaxLength(32);
        builder.HasIndex(e => e.Code).IsUnique();
        builder.Property(e => e.StudentUserId).HasColumnName("student_user_id");
        builder.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
