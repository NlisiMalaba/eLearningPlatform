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

        Module? nextGradeModule = modules.FirstOrDefault(m => m.Grade > current.Grade);
        if (nextGradeModule is null)
            return (false, null);

        List<Module> requiredPrior = modules
            .Where(m => m.IsRequired && m.Grade < nextGradeModule.Grade)
            .ToList();

        foreach (Module req in requiredPrior)
        {
            List<Guid> assessments = await db.Assessments
                .AsNoTracking()
                .Where(a => a.ModuleId == req.Id && a.TenantId == tenantId)
                .Select(a => a.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (Guid aid in assessments)
            {
                bool passed = await db.AssessmentAttempts
                    .AsNoTracking()
                    .AnyAsync(
                        a => a.TenantId == tenantId
                            && a.StudentId == studentId
                            && a.AssessmentId == aid
                            && a.SubmittedAt != null
                            && a.ScorePercent >= 60,
                        ct)
                    .ConfigureAwait(false);
                if (!passed)
                {
                    return (
                        true,
                        $"Complete required assessments in grade with score at least 60% before grade {nextGradeModule.Grade} modules.");
                }
            }
        }

        return (false, null);
    }
}
