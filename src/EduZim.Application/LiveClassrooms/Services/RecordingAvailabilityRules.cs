namespace EduZim.Application.LiveClassrooms.Services;

public static class RecordingAvailabilityRules
{
    public const int RetentionDays = 30;

    public static DateTime ExpiresAtUtc(DateTime sessionEndTime) => sessionEndTime.AddDays(RetentionDays);

    public static bool IsAccessible(DateTime sessionEndTime, DateTime utcNow) =>
        utcNow <= ExpiresAtUtc(sessionEndTime);
}
