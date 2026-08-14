using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.UserId, p.Type }).IsUnique();
        builder.Property(p => p.Type).HasConversion<int>();
    }
}
