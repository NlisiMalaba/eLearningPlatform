namespace EduZim.Application.Sync.DTOs;

public sealed record ProcessOfflineQueueResultDto(
    int AcceptedCount,
    int SyncedCount,
    int ConflictedCount);
