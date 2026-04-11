using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Billing.Services;
public sealed class SubscriptionRenewalReminderService : ISubscriptionRenewalReminderService
{
    private readonly IEduZimDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ILogger<SubscriptionRenewalReminderService> _logger;

    public SubscriptionRenewalReminderService(
        IEduZimDbContext db,
        IEmailService emailService,
        ISmsService smsService,
        ILogger<SubscriptionRenewalReminderService> logger)
    {
        _db = db;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken)
    {
        var tenantIds = await _db.Tenants.AsNoTracking()
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var windowEnd = now.AddDays(7);

        foreach (var tenantId in tenantIds)
        {
            await _db.SetSessionTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

            var tenantName = await _db.Tenants.AsNoTracking()
                .Where(t => t.Id == tenantId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrEmpty(tenantName))
                continue;

            var due = await _db.Subscriptions
                .Where(s =>
                    s.Status == SubscriptionStatus.Active
                    && s.CurrentPeriodEnd > now
                    && s.CurrentPeriodEnd <= windowEnd
                    && (s.RenewalReminderSentForPeriodEndUtc == null
                        || s.RenewalReminderSentForPeriodEndUtc != s.CurrentPeriodEnd))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var subscription in due)
            {
                var contacts = await _db.Users
                    .Where(u =>
                        u.TenantId == tenantId
                        && (u.Role == UserRole.SchoolAdmin || u.Role == UserRole.ParentGuardian))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (contacts.Count == 0)
                {
                    _logger.LogWarning(
                        "No SchoolAdmin/ParentGuardian users for tenant {TenantId}; skipping renewal reminder for subscription {SubscriptionId}.",
                        tenantId,
                        subscription.Id);
                    continue;
                }

                var periodEnd = subscription.CurrentPeriodEnd;
                foreach (var user in contacts)
                {
                    if (!string.IsNullOrWhiteSpace(user.Email))
                    {
                        var subject = $"EduZim subscription renews soon — {tenantName}";
                        var body =
                            $"<p>Your EduZim subscription for <strong>{System.Net.WebUtility.HtmlEncode(tenantName)}</strong> "
                            + $"renews on <strong>{periodEnd:yyyy-MM-dd} UTC</strong>.</p>"
                            + "<p>Please renew to avoid interruption.</p>";
                        await _emailService.SendAsync(user.Email!, subject, body, cancellationToken).ConfigureAwait(false);
                    }

                    if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                    {
                        var sms =
                            $"EduZim: {tenantName} subscription renews {periodEnd:yyyy-MM-dd} UTC. Renew to avoid interruption.";
                        if (sms.Length > 320)
                            sms = sms[..317] + "...";
                        await _smsService.SendAsync(user.PhoneNumber!, sms, cancellationToken).ConfigureAwait(false);
                    }
                }

                subscription.RenewalReminderSentForPeriodEndUtc = subscription.CurrentPeriodEnd;
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Queued renewal reminders for subscription {SubscriptionId}, tenant {TenantId}, period end {PeriodEndUtc}.",
                    subscription.Id,
                    tenantId,
                    periodEnd);
            }
        }
    }
}
