using MediatR;

namespace EduZim.Domain.Events;

public sealed record AssessmentAssignedNotification(Guid AssessmentClassAssignmentId, Guid TenantId) : INotification;
