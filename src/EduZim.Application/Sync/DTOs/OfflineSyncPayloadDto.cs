namespace EduZim.Application.Sync.DTOs;

public sealed record OfflineSyncPayloadDto(
    string Kind,
    Guid? ModuleId = null,
    bool IsCompleted = false,
    DateTime? CompletedAt = null,
    int TimeOnTaskSeconds = 0,
    Guid? AssessmentId = null,
    Guid? AttemptId = null,
    int? ScorePercent = null,
    int? TimeTakenSeconds = null,
    DateTime? SubmittedAt = null);
