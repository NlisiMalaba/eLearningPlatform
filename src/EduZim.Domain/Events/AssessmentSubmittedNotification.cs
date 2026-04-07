using MediatR;

namespace EduZim.Domain.Events;

public record AssessmentSubmittedNotification(
    Guid StudentId,
    Guid AssessmentId,
    Guid AssessmentAttemptId,
    Guid TenantId,
    int ScorePercent)
    : INotification;
