using EduZim.Domain.Enums;

namespace EduZim.Application.Progress.Services;

/// <summary>Module sequence unlock (correctness property 13).</summary>
public static class ModuleUnlockRules
{
    public readonly record struct ModuleSequenceRow(
        Guid Id,
        string Subject,
        GradeLevel Grade,
        int SequenceOrder);

    public static Guid? NextModuleId(IReadOnlyList<ModuleSequenceRow> modules, Guid completedModuleId)
    {
        List<ModuleSequenceRow> ordered = SequenceContaining(modules, completedModuleId);
        int index = ordered.FindIndex(m => m.Id == completedModuleId);
        if (index < 0 || index == ordered.Count - 1)
            return null;

        return ordered[index + 1].Id;
    }

    public static bool IsAccessible(
        IReadOnlyList<ModuleSequenceRow> modules,
        IReadOnlySet<Guid> completedModuleIds,
        IReadOnlySet<Guid> unlockedModuleIds,
        Guid moduleId)
    {
        if (unlockedModuleIds.Contains(moduleId))
            return true;

        List<ModuleSequenceRow> ordered = SequenceContaining(modules, moduleId);
        int index = ordered.FindIndex(m => m.Id == moduleId);
        if (index < 0)
            return false;
        if (index == 0)
            return true;

        return completedModuleIds.Contains(ordered[index - 1].Id);
    }

    private static List<ModuleSequenceRow> SequenceContaining(
        IReadOnlyList<ModuleSequenceRow> modules,
        Guid moduleId)
    {
        ModuleSequenceRow? current = null;
        foreach (ModuleSequenceRow row in modules)
        {
            if (row.Id == moduleId)
            {
                current = row;
                break;
            }
        }

        if (current is null)
            return [];

        ModuleSequenceRow match = current.Value;
        return modules
            .Where(m => m.Subject == match.Subject && m.Grade == match.Grade)
            .OrderBy(m => m.SequenceOrder)
            .ThenBy(m => m.Id)
            .ToList();
    }
}
