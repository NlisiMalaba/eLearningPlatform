using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Tenants;

internal sealed class NoOpTenantBackgroundJobs : ITenantBackgroundJobs
{
    public string? SchedulePermanentDeletionAt(Guid tenantId, DateTime runAtUtc) => "test-job-id";

    public bool TryCancelJob(string? hangfireJobId) => true;
}
