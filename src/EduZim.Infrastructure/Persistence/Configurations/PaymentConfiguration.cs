using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.SubscriptionId).HasColumnName("subscription_id");
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(8).HasDefaultValue("USD");
        builder.Property(p => p.ProviderReference).HasMaxLength(512);
        builder.Property(p => p.IdempotencyKey).HasMaxLength(256).HasColumnName("idempotency_key");
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(p => p.SubscriptionId);
        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
