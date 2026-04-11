using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Common.Interfaces;

public interface IEduZimDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<ContentItem> ContentItems { get; }
    DbSet<TenantInviteCode> TenantInviteCodes { get; }
    DbSet<ParentStudentLink> ParentStudentLinks { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<Payment> Payments { get; }
    DbSet<SubscriptionInvoice> SubscriptionInvoices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
