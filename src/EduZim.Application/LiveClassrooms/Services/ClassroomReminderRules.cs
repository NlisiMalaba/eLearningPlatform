namespace EduZim.Application.LiveClassrooms.Services;

public static class ClassroomReminderRules
{
    public static readonly TimeSpan LeadTime = TimeSpan.FromHours(24);

    public static DateTime NotifyAtOrBeforeUtc(DateTime startAtUtc) => startAtUtc - LeadTime;
}
