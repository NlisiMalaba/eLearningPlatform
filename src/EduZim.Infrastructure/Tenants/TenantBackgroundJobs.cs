using EduZim.Application.Common.Interfaces;
using EduZim.Infrastructure.Jobs;
using Hangfire;

namespace EduZim.Infrastructure.Tenants;

public sealed class TenantBackgroundJobs : ITenantBackgroundJobs
{
    public string? SchedulePermanentDeletionAt(Guid tenantId, DateTime runAtUtc)
    {
        var delay = runAtUtc - DateTime.UtcNow;
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        return BackgroundJob.Schedule<TenantPermanentDeletionJob>(
            job => job.RunAsync(tenantId),
            delay);
    }

    public bool TryCancelJob(string? hangfireJobId)
    {
        if (string.IsNullOrWhiteSpace(hangfireJobId))
            return false;
        return BackgroundJob.Delete(hangfireJobId);
    }
}
