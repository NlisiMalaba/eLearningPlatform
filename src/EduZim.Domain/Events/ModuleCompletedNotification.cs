using MediatR;

namespace EduZim.Domain.Events;

public record ModuleCompletedNotification(Guid StudentId, Guid ModuleId, Guid TenantId)
    : INotification;
