namespace EduZim.Application.Sync.DTOs;

public sealed record ResolveConflictResultDto(
    bool LocalWon,
    DateTime LocalTimestamp,
    DateTime ServerTimestamp,
    DateTime RetainedTimestamp);
