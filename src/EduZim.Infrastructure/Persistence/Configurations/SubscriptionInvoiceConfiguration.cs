using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduZim.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionInvoiceConfiguration : IEntityTypeConfiguration<SubscriptionInvoice>
{
    public void Configure(EntityTypeBuilder<SubscriptionInvoice> builder)
    {
        builder.ToTable("SubscriptionInvoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.SubscriptionId).HasColumnName("subscription_id");
        builder.Property(i => i.PaymentId).HasColumnName("payment_id");
        builder.Property(i => i.IssuedAtUtc).HasColumnName("issued_at_utc");
        builder.Property(i => i.PdfContent).HasColumnType("bytea");
        builder.Property(i => i.FileName).HasMaxLength(256);
        builder.HasIndex(i => i.SubscriptionId);
        builder.HasIndex(i => i.PaymentId).IsUnique();
        builder.HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(i => i.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(i => i.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
