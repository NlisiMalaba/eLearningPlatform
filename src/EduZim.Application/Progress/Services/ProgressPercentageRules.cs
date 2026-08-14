namespace EduZim.Application.Progress.Services;

/// <summary>Per-subject progress percentage (correctness property 14).</summary>
public static class ProgressPercentageRules
{
    public static int Calculate(int completedCount, int totalCount)
    {
        if (totalCount <= 0)
            return 0;

        return (int)Math.Round(100.0 * completedCount / totalCount, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<SubjectPercent> PerSubject(
        IReadOnlyList<ModuleSubjectRow> modules,
        IReadOnlySet<Guid> completedModuleIds)
    {
        return modules
            .GroupBy(m => m.Subject, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                int total = g.Count();
                int completed = g.Count(m => completedModuleIds.Contains(m.Id));
                return new SubjectPercent(g.Key, Calculate(completed, total));
            })
            .ToList();
    }

    public readonly record struct ModuleSubjectRow(Guid Id, string Subject);

    public readonly record struct SubjectPercent(string Subject, int ProgressPercent);
}
