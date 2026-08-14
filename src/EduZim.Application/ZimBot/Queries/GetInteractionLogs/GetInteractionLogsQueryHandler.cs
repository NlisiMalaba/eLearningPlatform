using EduZim.Application.Common.Interfaces;
using EduZim.Application.ZimBot.DTOs;
using EduZim.Application.ZimBot.Services;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.ZimBot.Queries.GetInteractionLogs;

public sealed class GetInteractionLogsQueryHandler
    : IRequestHandler<GetInteractionLogsQuery, ZimBotInteractionLogsDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetInteractionLogsQueryHandler> _logger;

    public GetInteractionLogsQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetInteractionLogsQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ZimBotInteractionLogsDto> Handle(GetInteractionLogsQuery request, CancellationToken ct)
    {
        ZimBotAccess.EnsureCanReviewLogs(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        IQueryable<ZimBotInteraction> query = _db.ZimBotInteractions
            .AsNoTracking()
            .Where(z => z.TenantId == request.TenantId);
        if (request.StudentId is Guid studentId)
            query = query.Where(z => z.StudentId == studentId);

        List<ZimBotInteraction> rows = await query
            .OrderByDescending(z => z.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "ZimBot logs queried for tenant {TenantId}: {Count} row(s).",
            request.TenantId,
            rows.Count);
        return new ZimBotInteractionLogsDto(request.TenantId, rows.Select(ZimBotMapper.ToLogDto).ToList());
    }
}
