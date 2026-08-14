using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Progress.Services;

internal static class PreschoolSessionApplier
{
    public static async Task<TenantTier?> LoadTierAsync(
        IEduZimDbContext db,
        Guid tenantId,
        CancellationToken ct)
    {
        Tenant? tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            .ConfigureAwait(false);
        return tenant?.Tier;
    }

    public static void ApplyHeartbeat(StudentSession session, TenantTier? tier, DateTime utcNow)
    {
        if (!PreschoolSessionRules.AppliesTo(tier) || session.Status != SessionStatus.Active)
            return;

        if (PreschoolSessionRules.ShouldPromptRest(session, utcNow))
            session.RestPromptRequired = true;

        if (PreschoolSessionRules.ShouldPauseForInactivity(session, utcNow))
            session.Status = SessionStatus.Paused;
    }

    public static void ApplyInteraction(StudentSession session, TenantTier? tier, DateTime utcNow)
    {
        session.LastInteractionAt = utcNow;
        session.UpdatedAt = utcNow;
        if (session.SegmentStartedAt is null)
            session.SegmentStartedAt = utcNow;

        if (!PreschoolSessionRules.AppliesTo(tier) || session.RestPromptRequired)
            return;

        if (PreschoolSessionRules.ShouldPromptRest(session, utcNow))
            session.RestPromptRequired = true;
    }

    public static void ApplyResume(StudentSession session, DateTime utcNow)
    {
        session.Status = SessionStatus.Active;
        session.LastInteractionAt = utcNow;
        session.LastHeartbeatAt = utcNow;
        session.SegmentStartedAt = utcNow;
        session.RestPromptRequired = false;
        session.UpdatedAt = utcNow;
    }
}
