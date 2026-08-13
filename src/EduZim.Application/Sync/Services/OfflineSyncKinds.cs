namespace EduZim.Application.Sync.Services;

public static class OfflineSyncKinds
{
    public const string ModuleProgress = "ModuleProgress";
    public const string AssessmentAttempt = "AssessmentAttempt";

    public static bool IsKnown(string? kind) =>
        kind is ModuleProgress or AssessmentAttempt;
}
