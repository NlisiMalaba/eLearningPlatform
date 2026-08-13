namespace EduZim.Application.ZimBot.DTOs;

public sealed record ZimBotChatDto(
    Guid InteractionId,
    string Reply,
    string Language,
    bool UsedHintMode,
    bool UsedFallback,
    bool IsLowConfidence);
