using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Common.Interfaces;

public interface IEduZimDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<ContentItem> ContentItems { get; }
    DbSet<Module> Modules { get; }
    DbSet<ModuleContentItem> ModuleContentItems { get; }
    DbSet<CaptionTrack> CaptionTracks { get; }
    DbSet<Transcript> Transcripts { get; }
    DbSet<TenantInviteCode> TenantInviteCodes { get; }
    DbSet<ParentStudentLink> ParentStudentLinks { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<Payment> Payments { get; }
    DbSet<SubscriptionInvoice> SubscriptionInvoices { get; }

    /// <summary>Sets PostgreSQL <c>app.current_tenant_id</c> for row-level security (e.g. Hangfire jobs).</summary>
    Task SetSessionTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
