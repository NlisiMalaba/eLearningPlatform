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

namespace EduZim.Application.AdaptiveLearning.Queries.GetAdjustedDifficulty;

public sealed class GetAdjustedDifficultyQueryHandler : IRequestHandler<GetAdjustedDifficultyQuery, AdjustedDifficultyDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetAdjustedDifficultyQueryHandler> _logger;

    public GetAdjustedDifficultyQueryHandler(
        IEduZimDbContext db,
        ICacheService cache,
        ICurrentUser currentUser,
        ILogger<GetAdjustedDifficultyQueryHandler> logger)
    {
        _db = db;
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AdjustedDifficultyDto> Handle(GetAdjustedDifficultyQuery request, CancellationToken ct)
    {
        bool parentLink = await ResolveParentLinkAsync(request, ct).ConfigureAwait(false);
        TenantAccessHelper.EnsureCanViewStudentAdaptiveData(
            _currentUser,
            request.TenantId,
            request.StudentId,
            parentLink);

        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        bool moduleExists = await _db.Modules
            .AsNoTracking()
            .AnyAsync(m => m.Id == request.ModuleId && m.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (!moduleExists)
            throw new NotFoundException(nameof(Module), request.ModuleId);

        string key = AdaptiveLearningCacheKeys.LearningProfile(request.TenantId, request.StudentId);
        StudentLearningProfileDto? profile = await _cache.GetAsync<StudentLearningProfileDto>(key, ct)
            .ConfigureAwait(false);

        int baseline = profile?.DifficultyTier ?? 3;
        int lastScore = profile?.LastScorePercent ?? 100;
        int adjusted = AdaptiveDifficulty.ComputeAdjustedTier(baseline, lastScore);

        _logger.LogDebug(
            "Adjusted difficulty for student {StudentId} module {ModuleId}: {Adjusted} (baseline {Baseline}).",
            request.StudentId,
            request.ModuleId,
            adjusted,
            baseline);

        return new AdjustedDifficultyDto(adjusted, baseline, lastScore);
    }

    private async Task<bool> ResolveParentLinkAsync(GetAdjustedDifficultyQuery request, CancellationToken ct)
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
}
