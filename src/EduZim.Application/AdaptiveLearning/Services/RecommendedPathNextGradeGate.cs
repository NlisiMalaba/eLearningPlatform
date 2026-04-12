using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.AdaptiveLearning.Services;

internal static class RecommendedPathNextGradeGate
{
    internal static async Task<(bool Locked, string? Reason)> EvaluateAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Module current,
        CancellationToken ct)
    {
        List<Module> modules = await db.Modules
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .OrderBy(m => m.Grade)
            .ThenBy(m => m.SequenceOrder)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<GradeAdvancementGateRules.ModuleGateRow> rows = modules
            .Select(m => new GradeAdvancementGateRules.ModuleGateRow(m.Id, m.Grade, m.IsRequired))
            .ToList();

        Module? nextGradeModule = null;
        foreach (Module m in modules)
        {
            if (m.Grade > current.Grade)
            {
                nextGradeModule = m;
                break;
            }
        }

        if (nextGradeModule is null)
            return (false, null);

        List<Module> requiredPrior = modules
            .Where(m => m.IsRequired && m.Grade < nextGradeModule.Grade)
            .ToList();

        Dictionary<Guid, IReadOnlyList<Guid>> assessmentIdsByModule = new();
        foreach (Module req in requiredPrior)
        {
            List<Guid> assessments = await db.Assessments
                .AsNoTracking()
                .Where(a => a.ModuleId == req.Id && a.TenantId == tenantId)
                .Select(a => a.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            assessmentIdsByModule[req.Id] = assessments;
        }

        List<Guid> allAssessmentIds = assessmentIdsByModule.Values.SelectMany(x => x).Distinct().ToList();
        HashSet<Guid> passedAssessmentIds;
        if (allAssessmentIds.Count == 0)
        {
            passedAssessmentIds = new HashSet<Guid>();
        }
        else
        {
            List<Guid> passed = await db.AssessmentAttempts
                .AsNoTracking()
                .Where(
                    a => a.TenantId == tenantId
                        && a.StudentId == studentId
                        && a.SubmittedAt != null
                        && a.ScorePercent >= 60
                        && allAssessmentIds.Contains(a.AssessmentId))
                .Select(a => a.AssessmentId)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            passedAssessmentIds = passed.ToHashSet();
        }

        return GradeAdvancementGateRules.Evaluate(rows, current.Id, assessmentIdsByModule, passedAssessmentIds);
    }
}
