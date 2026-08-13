using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Services;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Gamification.Queries.GetStudentBadges;

public sealed class GetStudentBadgesQueryHandler : IRequestHandler<GetStudentBadgesQuery, StudentBadgesDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetStudentBadgesQueryHandler> _logger;

    public GetStudentBadgesQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetStudentBadgesQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<StudentBadgesDto> Handle(GetStudentBadgesQuery request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        await GamificationStudentAccess
            .EnsureCanViewAsync(_db, _currentUser, request.TenantId, request.StudentId, ct)
            .ConfigureAwait(false);

        List<Badge> badges = await _db.Badges
            .AsNoTracking()
            .Where(b => b.TenantId == request.TenantId && b.StudentId == request.StudentId)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyList<StudentBadgeDto> items = badges
            .Select(b => new StudentBadgeDto(b.Id, b.Type, b.EarnedAt))
            .ToList();

        _logger.LogDebug(
            "Student badges queried for student {StudentId}: {BadgeCount} badge(s).",
            request.StudentId,
            items.Count);
        return new StudentBadgesDto(request.StudentId, items);
    }
}
