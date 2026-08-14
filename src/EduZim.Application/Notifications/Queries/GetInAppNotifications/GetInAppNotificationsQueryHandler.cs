using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.DTOs;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Queries.GetInAppNotifications;

public sealed class GetInAppNotificationsQueryHandler
    : IRequestHandler<GetInAppNotificationsQuery, InAppNotificationsDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetInAppNotificationsQueryHandler> _logger;

    public GetInAppNotificationsQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetInAppNotificationsQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<InAppNotificationsDto> Handle(GetInAppNotificationsQuery request, CancellationToken ct)
    {
        NotificationAccess.EnsureCanView(_currentUser, request.TenantId, request.UserId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        await EnsureUserExistsAsync(request, ct).ConfigureAwait(false);

        List<Notification> rows = await _db.Notifications
            .AsNoTracking()
            .Where(
                n => n.TenantId == request.TenantId
                    && n.UserId == request.UserId
                    && n.Channel == NotificationChannel.InApp)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        IReadOnlyList<InAppNotificationDto> items = rows.Select(NotificationMapper.ToInAppDto).ToList();
        _logger.LogDebug(
            "In-app notifications queried for user {UserId}: {Count} item(s).",
            request.UserId,
            items.Count);
        return new InAppNotificationsDto(request.UserId, items);
    }

    private async Task EnsureUserExistsAsync(GetInAppNotificationsQuery request, CancellationToken ct)
    {
        bool exists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId && u.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);
    }
}
