using System.ComponentModel.DataAnnotations;

namespace EduZim.API.Contracts;

public sealed class UploadOfflineQueueRequest
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public List<UploadOfflineQueueItemRequest> Items { get; set; } = [];
}

public sealed class UploadOfflineQueueItemRequest
{
    public Guid? ClientId { get; set; }

    [Required]
    public DateTime LocalTimestamp { get; set; }

    [Required]
    public UploadOfflineQueuePayloadRequest Payload { get; set; } = default!;
}

public sealed class UploadOfflineQueuePayloadRequest
{
    [Required]
    [MaxLength(64)]
    public string Kind { get; set; } = default!;

    public Guid? ModuleId { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int TimeOnTaskSeconds { get; set; }

    public Guid? AssessmentId { get; set; }

    public Guid? AttemptId { get; set; }

    public int? ScorePercent { get; set; }

    public int? TimeTakenSeconds { get; set; }

    public DateTime? SubmittedAt { get; set; }
}

public sealed class UploadOfflineQueueResponse
{
    public int AcceptedCount { get; init; }
    public int SyncedCount { get; init; }
    public int ConflictedCount { get; init; }
}
