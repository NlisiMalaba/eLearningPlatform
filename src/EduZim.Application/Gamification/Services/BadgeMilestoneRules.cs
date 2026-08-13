using EduZim.Domain.Enums;

namespace EduZim.Application.Gamification.Services;

/// <summary>Milestone evaluation for badge awards (correctness property 26).</summary>
public static class BadgeMilestoneRules
{
    public const int ConsecutiveDaysRequired = 5;

    public readonly record struct ModuleRow(Guid Id, string Subject, GradeLevel Grade);

    public static IReadOnlyList<BadgeType> DetermineNewAwards(
        IReadOnlyList<ModuleRow> tenantModules,
        IReadOnlySet<Guid> completedModuleIds,
        IReadOnlyCollection<DateOnly> activityDatesUtc,
        IReadOnlySet<BadgeType> alreadyEarned)
    {
        List<BadgeType> awards = new();

        if (!alreadyEarned.Contains(BadgeType.FirstModule) && completedModuleIds.Count >= 1)
            awards.Add(BadgeType.FirstModule);

        if (!alreadyEarned.Contains(BadgeType.FiveConsecutiveDays)
            && HasConsecutiveDayStreak(activityDatesUtc, ConsecutiveDaysRequired))
            awards.Add(BadgeType.FiveConsecutiveDays);

        if (!alreadyEarned.Contains(BadgeType.SubjectMastery)
            && HasCompletedAnyFullSubject(tenantModules, completedModuleIds))
            awards.Add(BadgeType.SubjectMastery);

        if (!alreadyEarned.Contains(BadgeType.GradeCompletion)
            && HasCompletedAnyFullGrade(tenantModules, completedModuleIds))
            awards.Add(BadgeType.GradeCompletion);

        return awards;
    }

    public static bool HasConsecutiveDayStreak(IReadOnlyCollection<DateOnly> activityDatesUtc, int requiredDays)
    {
        if (requiredDays <= 0 || activityDatesUtc.Count < requiredDays)
            return false;

        List<DateOnly> sorted = activityDatesUtc.Distinct().OrderBy(d => d).ToList();
        int streak = 1;
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] == sorted[i - 1].AddDays(1))
            {
                streak++;
                if (streak >= requiredDays)
                    return true;
            }
            else
            {
                streak = 1;
            }
        }

        return false;
    }

    private static bool HasCompletedAnyFullSubject(
        IReadOnlyList<ModuleRow> tenantModules,
        IReadOnlySet<Guid> completedModuleIds)
    {
        foreach (IGrouping<string, ModuleRow> group in tenantModules.GroupBy(
                     m => m.Subject,
                     StringComparer.OrdinalIgnoreCase))
        {
            if (GroupFullyCompleted(group, completedModuleIds))
                return true;
        }

        return false;
    }

    private static bool HasCompletedAnyFullGrade(
        IReadOnlyList<ModuleRow> tenantModules,
        IReadOnlySet<Guid> completedModuleIds)
    {
        foreach (IGrouping<GradeLevel, ModuleRow> group in tenantModules.GroupBy(m => m.Grade))
        {
            if (GroupFullyCompleted(group, completedModuleIds))
                return true;
        }

        return false;
    }

    private static bool GroupFullyCompleted(IEnumerable<ModuleRow> members, IReadOnlySet<Guid> completedModuleIds)
    {
        List<ModuleRow> list = members.ToList();
        return list.Count > 0 && list.All(m => completedModuleIds.Contains(m.Id));
    }
}
