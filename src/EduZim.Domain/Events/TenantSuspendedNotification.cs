using MediatR;

namespace EduZim.Domain.Events;

public record TenantSuspendedNotification(Guid TenantId, string? Reason)
    : INotification;
