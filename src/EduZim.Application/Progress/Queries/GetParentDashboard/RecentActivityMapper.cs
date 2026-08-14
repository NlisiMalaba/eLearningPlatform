using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;

namespace EduZim.Application.Progress.Queries.GetParentDashboard;

internal static class RecentActivityMapper
{
    public static IReadOnlyList<RecentActivityDto> Build(
        Guid studentId,
        IReadOnlyList<Module> modules,
        IReadOnlyList<StudentProgress> progress,
        IReadOnlyList<Badge> badges)
    {
        Dictionary<Guid, string> titles = modules.ToDictionary(m => m.Id, m => m.Title);
        List<RecentActivityDto> items = new();

        foreach (StudentProgress row in progress)
        {
            if (row.StudentId != studentId || row.CompletedAt is null)
                continue;

            string title = titles.TryGetValue(row.ModuleId, out string? name) ? name : "Module";
            items.Add(new RecentActivityDto("ModuleCompleted", title, row.CompletedAt.Value));
        }

        foreach (Badge badge in badges)
        {
            if (badge.StudentId != studentId)
                continue;

            items.Add(new RecentActivityDto("BadgeEarned", badge.Type.ToString(), badge.EarnedAt));
        }

        return items
            .OrderByDescending(a => a.OccurredAt)
            .Take(ParentDashboardRules.RecentActivityLimit)
            .ToList();
    }
}
