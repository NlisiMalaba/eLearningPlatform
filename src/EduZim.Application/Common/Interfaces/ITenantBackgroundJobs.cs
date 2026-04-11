namespace EduZim.Application.Common.Interfaces;

/// <summary>Schedules tenant lifecycle work (e.g. permanent deletion after suspension retention).</summary>
public interface ITenantBackgroundJobs
{
    /// <summary>Schedules permanent tenant data deletion at <paramref name="runAtUtc"/>.</summary>
    /// <returns>Background job id, or null if scheduling failed.</returns>
    string? SchedulePermanentDeletionAt(Guid tenantId, DateTime runAtUtc);

    /// <summary>Attempts to cancel a previously scheduled job (e.g. when a tenant is restored).</summary>
    bool TryCancelJob(string? hangfireJobId);
}
