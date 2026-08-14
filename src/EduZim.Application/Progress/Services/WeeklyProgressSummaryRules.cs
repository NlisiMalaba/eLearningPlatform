using System.Globalization;

namespace EduZim.Application.Progress.Services;

/// <summary>Weekly parent summary window and message (requirement 10.4).</summary>
public static class WeeklyProgressSummaryRules
{
    public static DateTime WeekStartUtc(DateTime utcNow)
    {
        DateTime date = utcNow.Date;
        int offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return DateTime.SpecifyKind(date.AddDays(-offset), DateTimeKind.Utc);
    }

    public static bool AlreadySentThisWeek(IReadOnlyList<DateTime> sentAtUtc, DateTime weekStartUtc)
    {
        foreach (DateTime sentAt in sentAtUtc)
        {
            if (sentAt >= weekStartUtc)
                return true;
        }

        return false;
    }

    public static string BuildMessage(IReadOnlyList<StudentWeekSummary> students)
    {
        if (students.Count == 0)
            return "EduZim weekly summary: no linked students this week.";

        IEnumerable<string> parts = students.Select(FormatStudent);
        return "EduZim weekly summary: " + string.Join(" ", parts);
    }

    private static string FormatStudent(StudentWeekSummary student)
    {
        string label = string.IsNullOrWhiteSpace(student.DisplayLabel)
            ? "Linked student"
            : student.DisplayLabel;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{label}: {student.ModulesCompleted} module(s) completed, {student.BadgesEarned} badge(s), progress {student.OverallProgressPercent}%.");
    }

    public readonly record struct StudentWeekSummary(
        string DisplayLabel,
        int ModulesCompleted,
        int BadgesEarned,
        int OverallProgressPercent);
}
