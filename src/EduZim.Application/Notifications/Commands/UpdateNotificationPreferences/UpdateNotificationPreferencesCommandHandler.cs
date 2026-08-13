using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Notifications.DTOs;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand, NotificationPreferencesDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateNotificationPreferencesCommandHandler> _logger;

    public UpdateNotificationPreferencesCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<UpdateNotificationPreferencesCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<NotificationPreferencesDto> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken ct)
    {
        NotificationAccess.EnsureCanUpdatePreferences(_currentUser, request.TenantId, request.UserId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        await EnsureUserExistsAsync(request, ct).ConfigureAwait(false);

        List<NotificationPreference> stored = await LoadPreferencesAsync(request, ct).ConfigureAwait(false);
        DateTime now = DateTime.UtcNow;
        await UpsertPreferencesAsync(request, stored, now, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Updated notification preferences for user {UserId}.", request.UserId);
        return new NotificationPreferencesDto(request.UserId, NotificationMapper.ToPreferenceDtos(stored));
    }

    private async Task EnsureUserExistsAsync(UpdateNotificationPreferencesCommand request, CancellationToken ct)
    {
        bool exists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId && u.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);
    }

    private async Task<List<NotificationPreference>> LoadPreferencesAsync(
        UpdateNotificationPreferencesCommand request,
        CancellationToken ct)
    {
        return await _db.NotificationPreferences
            .Where(p => p.TenantId == request.TenantId && p.UserId == request.UserId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task UpsertPreferencesAsync(
        UpdateNotificationPreferencesCommand request,
        List<NotificationPreference> stored,
        DateTime now,
        CancellationToken ct)
    {
        Dictionary<NotificationType, NotificationPreference> byType =
            stored.ToDictionary(p => p.Type);

        foreach (NotificationPreferenceItemDto item in request.Preferences)
        {
            if (byType.TryGetValue(item.Type, out NotificationPreference? existing))
            {
                existing.InAppEnabled = item.InAppEnabled;
                existing.EmailEnabled = item.EmailEnabled;
                existing.SmsEnabled = item.SmsEnabled;
                existing.UpdatedAt = now;
                continue;
            }

            await AddPreferenceAsync(request, stored, item, now, ct).ConfigureAwait(false);
        }
    }

    private async Task AddPreferenceAsync(
        UpdateNotificationPreferencesCommand request,
        List<NotificationPreference> stored,
        NotificationPreferenceItemDto item,
        DateTime now,
        CancellationToken ct)
    {
        var row = new NotificationPreference
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Type = item.Type,
            InAppEnabled = item.InAppEnabled,
            EmailEnabled = item.EmailEnabled,
            SmsEnabled = item.SmsEnabled,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _db.NotificationPreferences.AddAsync(row, ct).ConfigureAwait(false);
        stored.Add(row);
    }
}
