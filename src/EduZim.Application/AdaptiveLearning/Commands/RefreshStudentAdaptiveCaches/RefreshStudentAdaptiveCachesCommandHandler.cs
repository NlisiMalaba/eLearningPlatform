using EduZim.Application.AdaptiveLearning.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.AdaptiveLearning.Commands.RefreshStudentAdaptiveCaches;

public sealed class RefreshStudentAdaptiveCachesCommandHandler : IRequestHandler<RefreshStudentAdaptiveCachesCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RefreshStudentAdaptiveCachesCommandHandler> _logger;

    public RefreshStudentAdaptiveCachesCommandHandler(
        IEduZimDbContext db,
        ICacheService cache,
        ICurrentUser currentUser,
        ILogger<RefreshStudentAdaptiveCachesCommandHandler> logger)
    {
        _db = db;
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(RefreshStudentAdaptiveCachesCommand request, CancellationToken ct)
    {
        bool parentLink = await ResolveParentLinkAsync(request, ct).ConfigureAwait(false);
        TenantAccessHelper.EnsureCanViewStudentAdaptiveData(
            _currentUser,
            request.TenantId,
            request.StudentId,
            parentLink);

        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        bool updated = await AdaptiveLearningProfileCacheUpdater.TryRefreshFromLatestSubmittedAttemptAsync(
                _db,
                _cache,
                request.TenantId,
                request.StudentId,
                ct)
            .ConfigureAwait(false);

        await AdaptiveLearningProfileCacheUpdater
            .InvalidateCurrentWeeklySummaryAsync(_cache, request.TenantId, request.StudentId, ct)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Refreshed adaptive caches for student {StudentId} (profile from latest attempt: {Updated}).",
            request.StudentId,
            updated);

        return Unit.Value;
    }

    private async Task<bool> ResolveParentLinkAsync(RefreshStudentAdaptiveCachesCommand request, CancellationToken ct)
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
