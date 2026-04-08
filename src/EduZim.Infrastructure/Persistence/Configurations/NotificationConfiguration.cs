using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.TenantId).HasColumnName("tenant_id");
        builder.HasIndex(n => n.TenantId);
        builder.Property(n => n.Type).HasConversion<int>();
        builder.Property(n => n.Channel).HasConversion<int>();
        builder.Property(n => n.Status).HasConversion<int>();
        builder.Property(n => n.Message).HasMaxLength(4000);
    }
}
