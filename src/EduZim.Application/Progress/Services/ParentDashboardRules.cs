using EduZim.Domain.Enums;

namespace EduZim.Application.Progress.Services;

/// <summary>Derives current grade, subjects, and overall percent for parent dashboards (property 29).</summary>
public static class ParentDashboardRules
{
    public const int RecentActivityLimit = 10;

    public readonly record struct ModuleGradeRow(Guid Id, GradeLevel Grade, string Subject);

    public static GradeLevel ResolveCurrentGrade(
        IReadOnlyList<ModuleGradeRow> modules,
        IReadOnlySet<Guid> completedModuleIds)
    {
        if (modules.Count == 0)
            return GradeLevel.EcdGrade0;

        List<ModuleGradeRow> incomplete = modules
            .Where(m => !completedModuleIds.Contains(m.Id))
            .ToList();
        if (incomplete.Count > 0)
            return incomplete.Min(m => m.Grade);

        return modules.Max(m => m.Grade);
    }

    public static IReadOnlyList<string> SubjectsForGrade(
        IReadOnlyList<ModuleGradeRow> modules,
        GradeLevel currentGrade)
    {
        return modules
            .Where(m => m.Grade == currentGrade)
            .Select(m => m.Subject)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
    }
}
