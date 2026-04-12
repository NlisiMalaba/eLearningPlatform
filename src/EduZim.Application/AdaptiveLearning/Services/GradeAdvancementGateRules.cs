using EduZim.Domain.Enums;

namespace EduZim.Application.AdaptiveLearning.Services;

/// <summary>Pure grade-advancement gate (correctness property 17).</summary>
public static class GradeAdvancementGateRules
{
    public readonly record struct ModuleGateRow(Guid Id, GradeLevel Grade, bool IsRequired);

    public static (bool Locked, string? Reason) Evaluate(
        IReadOnlyList<ModuleGateRow> modulesOrdered,
        Guid currentModuleId,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> assessmentIdsByModuleId,
        IReadOnlySet<Guid> passedAssessmentIds)
    {
        if (!modulesOrdered.Any(m => m.Id == currentModuleId))
            return (false, null);

        ModuleGateRow current = modulesOrdered.First(m => m.Id == currentModuleId);

        ModuleGateRow? nextGradeModule = null;
        foreach (ModuleGateRow m in modulesOrdered)
        {
            if (m.Grade > current.Grade)
            {
                nextGradeModule = m;
                break;
            }
        }

        if (nextGradeModule is null)
            return (false, null);

        GradeLevel nextGrade = nextGradeModule.Value.Grade;

        List<ModuleGateRow> requiredPrior = modulesOrdered
            .Where(m => m.IsRequired && m.Grade < nextGrade)
            .ToList();

        foreach (ModuleGateRow req in requiredPrior)
        {
            if (!assessmentIdsByModuleId.TryGetValue(req.Id, out IReadOnlyList<Guid>? assessments))
                continue;

            foreach (Guid aid in assessments)
            {
                if (!passedAssessmentIds.Contains(aid))
                {
                    return (
                        true,
                        $"Complete required assessments in grade with score at least 60% before grade {nextGrade} modules.");
                }
            }
        }

        return (false, null);
    }
}
