using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Assessments.Queries.ListAssessments;

public sealed class ListAssessmentsQueryHandler
    : IRequestHandler<ListAssessmentsQuery, IReadOnlyList<AssessmentListItemDto>>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListAssessmentsQueryHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AssessmentListItemDto>> Handle(ListAssessmentsQuery request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        List<Assessment> assessments = await _db.Assessments.AsNoTracking()
            .Where(a => a.TenantId == request.TenantId)
            .OrderBy(a => a.Title)
            .ThenBy(a => a.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Dictionary<Guid, int> counts = await LoadQuestionCountsAsync(request.TenantId, ct).ConfigureAwait(false);
        return assessments
            .Select(a => new AssessmentListItemDto(
                a.Id,
                a.Title,
                a.ModuleId,
                a.TimeLimitSeconds,
                a.PassingScorePercent,
                counts.GetValueOrDefault(a.Id)))
            .ToList();
    }

    private async Task<Dictionary<Guid, int>> LoadQuestionCountsAsync(Guid tenantId, CancellationToken ct)
    {
        List<Question> questions = await _db.Questions.AsNoTracking()
            .Where(q => q.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return questions
            .GroupBy(q => q.AssessmentId)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
