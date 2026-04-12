using System.Globalization;
using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.AdaptiveLearning.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.AdaptiveLearning.Queries.GetWeeklySummary;

public sealed class GetWeeklySummaryQueryHandler : IRequestHandler<GetWeeklySummaryQuery, WeeklyAdaptiveSummaryDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetWeeklySummaryQueryHandler> _logger;

    public GetWeeklySummaryQueryHandler(
        IEduZimDbContext db,
        ICacheService cache,
        ICurrentUser currentUser,
        ILogger<GetWeeklySummaryQueryHandler> logger)
    {
        _db = db;
        _cache = cache;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<WeeklyAdaptiveSummaryDto> Handle(GetWeeklySummaryQuery request, CancellationToken ct)
    {
        bool parentLink = await ResolveParentLinkAsync(request, ct).ConfigureAwait(false);
        TenantAccessHelper.EnsureCanViewStudentAdaptiveData(
            _currentUser,
            request.TenantId,
            request.StudentId,
            parentLink);

        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        DateTime referenceUtc = DateTime.UtcNow;
        int isoYear = ISOWeek.GetYear(referenceUtc);
        int isoWeek = ISOWeek.GetWeekOfYear(referenceUtc);
        string key = AdaptiveLearningCacheKeys.WeeklySummary(request.TenantId, request.StudentId, isoYear, isoWeek);

        WeeklyAdaptiveSummaryDto? cached = await _cache.GetAsync<WeeklyAdaptiveSummaryDto>(key, ct)
            .ConfigureAwait(false);
        if (cached is not null)
            return cached;

        WeeklyAdaptiveSummaryDto dto = await WeeklyAdaptiveSummaryBuilder
            .BuildAsync(_db, request.TenantId, request.StudentId, referenceUtc, ct)
            .ConfigureAwait(false);

        await _cache.SetAsync(key, dto, TimeSpan.FromDays(14), ct).ConfigureAwait(false);

        _logger.LogDebug("Weekly adaptive summary served for student {StudentId}.", request.StudentId);

        return dto;
    }

    private async Task<bool> ResolveParentLinkAsync(GetWeeklySummaryQuery request, CancellationToken ct)
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
