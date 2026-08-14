using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;

namespace EduZim.Application.Progress.Commands.SendParentWeeklySummaries;

internal static class WeeklySummaryComposer
{
    public static string Compose(
        IReadOnlyList<Guid> studentIds,
        IReadOnlyList<Module> modules,
        IReadOnlyList<StudentProgress> progress,
        IReadOnlyList<Badge> badges,
        IReadOnlyDictionary<Guid, string> displayLabels,
        DateTime weekStartUtc)
    {
        List<WeeklyProgressSummaryRules.StudentWeekSummary> summaries = studentIds
            .Select(id => Summarise(id, modules, progress, badges, displayLabels, weekStartUtc))
            .ToList();
        return WeeklyProgressSummaryRules.BuildMessage(summaries);
    }

    private static WeeklyProgressSummaryRules.StudentWeekSummary Summarise(
        Guid studentId,
        IReadOnlyList<Module> modules,
        IReadOnlyList<StudentProgress> progress,
        IReadOnlyList<Badge> badges,
        IReadOnlyDictionary<Guid, string> displayLabels,
        DateTime weekStartUtc)
    {
        HashSet<Guid> completedIds = progress
            .Where(p => p.StudentId == studentId && p.IsCompleted)
            .Select(p => p.ModuleId)
            .ToHashSet();
        int overall = ProgressPercentageRules.Calculate(completedIds.Count, modules.Count);
        int modulesThisWeek = progress.Count(
            p => p.StudentId == studentId
                && p.IsCompleted
                && p.CompletedAt is DateTime at
                && at >= weekStartUtc);
        int badgesThisWeek = badges.Count(b => b.StudentId == studentId && b.EarnedAt >= weekStartUtc);
        string label = displayLabels.TryGetValue(studentId, out string? name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : "Linked student";
        return new WeeklyProgressSummaryRules.StudentWeekSummary(label, modulesThisWeek, badgesThisWeek, overall);
    }
}
