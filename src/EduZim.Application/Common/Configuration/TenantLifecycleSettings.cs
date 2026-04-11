namespace EduZim.Application.Common.Configuration;

public sealed class TenantLifecycleSettings
{
    public const string SectionName = "TenantLifecycle";

    public int InviteCodeLifetimeDays { get; set; } = 7;

    public int SuspendedTenantRetentionDays { get; set; } = 90;
}
