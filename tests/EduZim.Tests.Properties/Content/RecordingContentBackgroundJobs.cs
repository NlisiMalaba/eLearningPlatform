using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Content;

internal sealed class RecordingContentBackgroundJobs : IContentBackgroundJobs
{
    public List<(Guid TenantId, Guid ContentItemId, DateTime RunAtUtc)> Scheduled { get; } = new();

    public string? SchedulePermanentDeletionAt(Guid tenantId, Guid contentItemId, DateTime runAtUtc)
    {
        Scheduled.Add((tenantId, contentItemId, runAtUtc));
        return "job-" + Guid.NewGuid().ToString("N");
    }

    public bool TryCancelJob(string? hangfireJobId) => true;
}
