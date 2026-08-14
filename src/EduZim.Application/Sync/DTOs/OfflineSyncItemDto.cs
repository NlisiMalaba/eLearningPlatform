namespace EduZim.Application.Sync.DTOs;

public sealed record OfflineSyncItemDto(
    Guid? ClientId,
    DateTime LocalTimestamp,
    OfflineSyncPayloadDto Payload);
