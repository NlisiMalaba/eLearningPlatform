namespace EduZim.Application.ZimBot.Services;

public static class ZimBotReplyComposer
{
    public static (string Reply, bool UsedFallback, bool IsLowConfidence) Compose(string raw)
    {
        if (ZimBotMessages.IsUnavailable(raw))
            return (ZimBotMessages.Unavailable, true, false);

        (bool isLowConfidence, string body) = ZimBotResponseParser.Parse(raw);
        if (isLowConfidence && string.IsNullOrWhiteSpace(body))
            return (ZimBotMessages.AskTeacher, false, true);

        return (body, false, isLowConfidence);
    }
}
