namespace EduZim.Application.Notifications.Services;

public static class InactivityAlertRules
{
    public const int InactiveAfterDays = 7;

    public static DateTime CutoffUtc(DateTime utcNow) => utcNow.AddDays(-InactiveAfterDays);

    public static bool IsInactive(DateTime? lastLoginAt, DateTime utcNow)
    {
        if (lastLoginAt is null)
            return false;

        return lastLoginAt.Value <= CutoffUtc(utcNow);
    }

    public static bool NeedsAlert(DateTime? lastLoginAt, DateTime? lastInactivityAlertAt, DateTime utcNow)
    {
        if (lastLoginAt is not DateTime loginAt)
            return false;
        if (loginAt > CutoffUtc(utcNow))
            return false;
        if (lastInactivityAlertAt is null)
            return true;

        return lastInactivityAlertAt.Value < loginAt;
    }

    public static string ParentMessage() =>
        "Your linked student has not logged in for 7 days. Encourage them to continue learning on EduZim.";
}
