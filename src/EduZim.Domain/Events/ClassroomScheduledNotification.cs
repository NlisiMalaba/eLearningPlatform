using MediatR;

namespace EduZim.Domain.Events;

public sealed record ClassroomScheduledNotification(Guid SessionId, Guid TenantId) : INotification;
