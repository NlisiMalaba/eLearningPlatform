using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Domain.Events;

public record BadgeAwardedNotification(Guid StudentId, Guid BadgeId, Guid TenantId, BadgeType BadgeType)
    : INotification;
