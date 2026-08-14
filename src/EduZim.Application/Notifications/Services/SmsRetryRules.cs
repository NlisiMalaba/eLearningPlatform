namespace EduZim.Application.Notifications.Services;

public static class SmsRetryRules
{
    public const int MaxRetryAttempts = 3;
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(10);

    public static bool CanScheduleAnotherRetry(int retryCount) => retryCount < MaxRetryAttempts;
}
