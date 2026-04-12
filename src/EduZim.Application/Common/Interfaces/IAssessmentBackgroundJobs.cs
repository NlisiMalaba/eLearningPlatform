namespace EduZim.Application.Common.Interfaces;

/// <summary>Schedules timed assessment auto-submit at the end of the allowed window.</summary>
public interface IAssessmentBackgroundJobs
{
    string? ScheduleTimedAutoSubmitAt(Guid tenantId, Guid attemptId, DateTime runAtUtc);

    bool TryCancelJob(string? hangfireJobId);
}
