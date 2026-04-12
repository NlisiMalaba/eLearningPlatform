namespace EduZim.Application.AdaptiveLearning.Services;

internal static class AdaptiveLearningCacheKeys
{
    internal static string LearningProfile(Guid tenantId, Guid studentId) =>
        $"{tenantId}:adaptive:profile:{studentId}";

    internal static string WeeklySummary(Guid tenantId, Guid studentId, int isoYear, int isoWeek) =>
        $"{tenantId}:adaptive:weekly:{studentId}:y{isoYear}-W{isoWeek:D2}";
}
