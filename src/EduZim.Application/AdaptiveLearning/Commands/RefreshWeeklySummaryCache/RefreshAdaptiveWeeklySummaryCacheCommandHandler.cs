using EduZim.Application.AdaptiveLearning.DTOs;
using EduZim.Application.AdaptiveLearning.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.AdaptiveLearning.Commands.RefreshWeeklySummaryCache;

public sealed class RefreshAdaptiveWeeklySummaryCacheCommandHandler : IRequestHandler<RefreshAdaptiveWeeklySummaryCacheCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<RefreshAdaptiveWeeklySummaryCacheCommandHandler> _logger;

    public RefreshAdaptiveWeeklySummaryCacheCommandHandler(
        IEduZimDbContext db,
        ICacheService cache,
        ILogger<RefreshAdaptiveWeeklySummaryCacheCommandHandler> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(RefreshAdaptiveWeeklySummaryCacheCommand request, CancellationToken ct)
    {
        List<Guid> tenantIds = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        DateTime referenceUtc = DateTime.UtcNow;

        foreach (Guid tenantId in tenantIds)
        {
            await RefreshTenantAsync(tenantId, referenceUtc, ct).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    private async Task RefreshTenantAsync(Guid tenantId, DateTime referenceUtc, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(tenantId, ct).ConfigureAwait(false);

        List<Guid> studentIds = await _db.ClassEnrollments
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .Select(e => e.StudentUserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (Guid studentId in studentIds)
        {
            WeeklyAdaptiveSummaryDto dto = await WeeklyAdaptiveSummaryBuilder
                .BuildAsync(_db, tenantId, studentId, referenceUtc, ct)
                .ConfigureAwait(false);

            string key = AdaptiveLearningCacheKeys.WeeklySummary(tenantId, studentId, dto.IsoYear, dto.IsoWeek);
            await _cache.SetAsync(key, dto, TimeSpan.FromDays(14), ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Refreshed weekly adaptive summary cache for tenant {TenantId} ({Count} students).",
            tenantId,
            studentIds.Count);
    }
}
