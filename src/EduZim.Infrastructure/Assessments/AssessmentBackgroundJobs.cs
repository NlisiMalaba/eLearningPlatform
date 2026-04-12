using EduZim.Application.Common.Interfaces;
using EduZim.Infrastructure.Jobs;
using Hangfire;

namespace EduZim.Infrastructure.Assessments;

public sealed class AssessmentBackgroundJobs : IAssessmentBackgroundJobs
{
    public string? ScheduleTimedAutoSubmitAt(Guid tenantId, Guid attemptId, DateTime runAtUtc)
    {
        TimeSpan delay = runAtUtc - DateTime.UtcNow;
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        return BackgroundJob.Schedule<AssessmentTimedAutoSubmitJob>(
            job => job.RunAsync(tenantId, attemptId),
            delay);
    }

    public bool TryCancelJob(string? hangfireJobId)
    {
        if (string.IsNullOrWhiteSpace(hangfireJobId))
            return false;
        return BackgroundJob.Delete(hangfireJobId);
    }
}
