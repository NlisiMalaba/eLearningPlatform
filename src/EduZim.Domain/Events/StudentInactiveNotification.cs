using MediatR;

namespace EduZim.Domain.Events;

public record StudentInactiveNotification(
    Guid StudentId,
    Guid TenantId,
    DateTime? LastLoginAt)
    : INotification;
