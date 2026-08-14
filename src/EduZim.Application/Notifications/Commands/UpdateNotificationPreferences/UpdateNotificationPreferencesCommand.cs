using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.DTOs;
using MediatR;

namespace EduZim.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    Guid TenantId,
    Guid UserId,
    IReadOnlyList<NotificationPreferenceItemDto> Preferences)
    : IRequest<NotificationPreferencesDto>, ITenantScopedRequest;
