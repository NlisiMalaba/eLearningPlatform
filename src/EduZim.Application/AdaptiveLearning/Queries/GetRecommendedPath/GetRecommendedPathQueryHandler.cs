using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.AdaptiveLearning.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.AdaptiveLearning.Queries.GetRecommendedPath;

public sealed class GetRecommendedPathQueryHandler : IRequestHandler<GetRecommendedPathQuery, RecommendedPathDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetRecommendedPathQueryHandler> _logger;

    public GetRecommendedPathQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetRecommendedPathQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<RecommendedPathDto> Handle(GetRecommendedPathQuery request, CancellationToken ct)
    {
        bool parentLink = await ResolveParentLinkAsync(request, ct).ConfigureAwait(false);
        TenantAccessHelper.EnsureCanViewStudentAdaptiveData(
            _currentUser,
            request.TenantId,
            request.StudentId,
            parentLink);

        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        Module? module = await _db.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.ModuleId && m.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (module is null)
            throw new NotFoundException(nameof(Module), request.ModuleId);

        int? latestScore = await LatestModuleAssessmentScoreAsync(request, ct).ConfigureAwait(false);
        List<PathItemRow> orderedItems = await LoadOrderedContentRowsAsync(request, ct).ConfigureAwait(false);

        IReadOnlyList<RecommendedPathItemDto> remedial = BuildRemedial(orderedItems, latestScore);
        IReadOnlyList<RecommendedPathItemDto> advanced = BuildAdvanced(orderedItems, latestScore);

        (bool locked, string? reason) = await RecommendedPathNextGradeGate
            .EvaluateAsync(_db, request.TenantId, request.StudentId, module, ct)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Recommended path for student {StudentId} module {ModuleId}: remedial {RemedialCount}, advanced {AdvCount}.",
            request.StudentId,
            request.ModuleId,
            remedial.Count,
            advanced.Count);

        return new RecommendedPathDto(
            request.ModuleId,
            latestScore,
            remedial,
            advanced,
            locked,
            reason);
    }

    private async Task<bool> ResolveParentLinkAsync(GetRecommendedPathQuery request, CancellationToken ct)
    {
        if (_currentUser.Role != UserRole.ParentGuardian)
            return true;

        return await _db.ParentStudentLinks
            .AsNoTracking()
            .AnyAsync(
                l => l.TenantId == request.TenantId
                    && l.ParentUserId == _currentUser.UserId
                    && l.StudentUserId == request.StudentId,
                ct)
            .ConfigureAwait(false);
    }

    private async Task<int?> LatestModuleAssessmentScoreAsync(GetRecommendedPathQuery request, CancellationToken ct)
    {
        List<Guid> assessmentIds = await _db.Assessments
            .AsNoTracking()
            .Where(a => a.ModuleId == request.ModuleId && a.TenantId == request.TenantId)
            .Select(a => a.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (assessmentIds.Count == 0)
            return null;

        int? best = await _db.AssessmentAttempts
            .AsNoTracking()
            .Where(
                a => a.TenantId == request.TenantId
                    && a.StudentId == request.StudentId
                    && a.SubmittedAt != null
                    && assessmentIds.Contains(a.AssessmentId))
            .MaxAsync(a => (int?)a.ScorePercent, ct)
            .ConfigureAwait(false);

        return best;
    }

    private async Task<List<PathItemRow>> LoadOrderedContentRowsAsync(
        GetRecommendedPathQuery request,
        CancellationToken ct)
    {
        return await _db.ModuleContentItems
            .AsNoTracking()
            .Where(m => m.ModuleId == request.ModuleId && m.TenantId == request.TenantId)
            .Join(
                _db.ContentItems.AsNoTracking(),
                m => m.ContentItemId,
                c => c.Id,
                (m, c) => new PathItemRow(m.SequenceOrder, c.Id, c.Title))
            .OrderBy(x => x.SequenceOrder)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static IReadOnlyList<RecommendedPathItemDto> BuildRemedial(
        IReadOnlyList<PathItemRow> ordered,
        int? latestScore)
    {
        if (ordered.Count == 0 || latestScore is null || latestScore >= 60)
            return Array.Empty<RecommendedPathItemDto>();

        int take = Math.Max(1, ordered.Count / 2);
        return ordered
            .Take(take)
            .Select(
                x => new RecommendedPathItemDto(
                    x.ContentItemId,
                    x.Title,
                    "Remedial reinforcement (score under 60%)."))
            .ToList();
    }

    private static IReadOnlyList<RecommendedPathItemDto> BuildAdvanced(
        IReadOnlyList<PathItemRow> ordered,
        int? latestScore)
    {
        if (ordered.Count == 0 || latestScore is null || latestScore < 85)
            return Array.Empty<RecommendedPathItemDto>();

        int take = Math.Min(2, ordered.Count);
        return ordered
            .Skip(Math.Max(0, ordered.Count - take))
            .Select(
                x => new RecommendedPathItemDto(
                    x.ContentItemId,
                    x.Title,
                    "Advanced extension (score at or above 85%)."))
            .ToList();
    }

    private sealed record PathItemRow(int SequenceOrder, Guid ContentItemId, string Title);
}
