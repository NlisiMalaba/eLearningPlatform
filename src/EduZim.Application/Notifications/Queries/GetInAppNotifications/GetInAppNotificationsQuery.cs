using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.DTOs;
using MediatR;

namespace EduZim.Application.Notifications.Queries.GetInAppNotifications;

public sealed record GetInAppNotificationsQuery(Guid TenantId, Guid UserId)
    : IRequest<InAppNotificationsDto>, ITenantScopedRequest;
