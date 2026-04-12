using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Assessments;

internal sealed class NoOpAssessmentBackgroundJobs : IAssessmentBackgroundJobs
{
    public string? ScheduleTimedAutoSubmitAt(Guid tenantId, Guid attemptId, DateTime runAtUtc) => "test-job-id";

    public bool TryCancelJob(string? hangfireJobId) => true;
}
