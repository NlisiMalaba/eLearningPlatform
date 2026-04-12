using System.Globalization;
using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.AdaptiveLearning.Services;

internal static class WeeklyAdaptiveSummaryBuilder
{
    internal static async Task<WeeklyAdaptiveSummaryDto> BuildAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        DateTime referenceUtc,
        CancellationToken ct)
    {
        int isoYear = ISOWeek.GetYear(referenceUtc);
        int isoWeek = ISOWeek.GetWeekOfYear(referenceUtc);
        DateTime weekStart = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);
        DateTime weekEnd = weekStart.AddDays(7).AddTicks(-1);

        List<AssessmentAttempt> attempts = await db.AssessmentAttempts
            .AsNoTracking()
            .Where(
                a => a.TenantId == tenantId
                    && a.StudentId == studentId
                    && a.SubmittedAt != null
                    && a.SubmittedAt >= weekStart
                    && a.SubmittedAt <= weekEnd)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        int assessmentCount = attempts.Count;
        double? avg = attempts.Count == 0
            ? null
            : attempts.Average(a => (double)a.ScorePercent);
        int timeSeconds = attempts.Sum(a => a.TimeTakenSeconds);

        int modulesDone = await db.StudentProgresses
            .AsNoTracking()
            .Where(
                p => p.TenantId == tenantId
                    && p.StudentId == studentId
                    && p.IsCompleted
                    && p.CompletedAt != null
                    && p.CompletedAt >= weekStart
                    && p.CompletedAt <= weekEnd)
            .CountAsync(ct)
            .ConfigureAwait(false);

        return new WeeklyAdaptiveSummaryDto(
            isoYear,
            isoWeek,
            weekStart,
            weekEnd,
            assessmentCount,
            avg,
            timeSeconds,
            modulesDone);
    }
}
