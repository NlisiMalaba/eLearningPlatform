using EduZim.Application.ZimBot.DTOs;
using EduZim.Domain.Entities;

namespace EduZim.Application.ZimBot.Services;

public static class ZimBotMapper
{
    public static ZimBotChatDto ToChatDto(ZimBotInteraction row) =>
        new(
            row.Id,
            row.Response,
            row.Language,
            row.UsedHintMode,
            row.UsedFallback,
            row.IsLowConfidence);

    public static ZimBotInteractionLogDto ToLogDto(ZimBotInteraction row) =>
        new(
            row.Id,
            row.StudentId,
            row.ModuleId,
            row.Question,
            row.Response,
            row.Language,
            row.UsedHintMode,
            row.UsedFallback,
            row.IsLowConfidence,
            row.CreatedAt);
}
