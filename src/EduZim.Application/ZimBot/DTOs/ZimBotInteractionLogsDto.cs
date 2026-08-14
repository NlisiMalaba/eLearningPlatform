namespace EduZim.Application.ZimBot.DTOs;

public sealed record ZimBotInteractionLogDto(
    Guid Id,
    Guid StudentId,
    Guid? ModuleId,
    string Question,
    string Response,
    string Language,
    bool UsedHintMode,
    bool UsedFallback,
    bool IsLowConfidence,
    DateTime CreatedAt);

public sealed record ZimBotInteractionLogsDto(
    Guid TenantId,
    IReadOnlyList<ZimBotInteractionLogDto> Items);
