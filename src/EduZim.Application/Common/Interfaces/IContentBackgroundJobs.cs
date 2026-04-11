namespace EduZim.Application.Common.Interfaces;

/// <summary>Schedules permanent deletion of archived content after retention.</summary>
public interface IContentBackgroundJobs
{
    string? SchedulePermanentDeletionAt(Guid tenantId, Guid contentItemId, DateTime runAtUtc);

    bool TryCancelJob(string? hangfireJobId);
}
