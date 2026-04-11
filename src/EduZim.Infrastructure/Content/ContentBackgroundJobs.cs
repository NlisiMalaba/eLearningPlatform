using EduZim.Application.Common.Interfaces;
using EduZim.Infrastructure.Jobs;
using Hangfire;

namespace EduZim.Infrastructure.Content;

public sealed class ContentBackgroundJobs : IContentBackgroundJobs
{
    public string? SchedulePermanentDeletionAt(Guid tenantId, Guid contentItemId, DateTime runAtUtc)
    {
        var delay = runAtUtc - DateTime.UtcNow;
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        return BackgroundJob.Schedule<ContentPermanentDeletionJob>(
            job => job.RunAsync(tenantId, contentItemId),
            delay);
    }

    public bool TryCancelJob(string? hangfireJobId)
    {
        if (string.IsNullOrWhiteSpace(hangfireJobId))
            return false;
        return BackgroundJob.Delete(hangfireJobId);
    }
}
