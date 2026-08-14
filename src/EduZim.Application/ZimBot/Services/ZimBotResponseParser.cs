namespace EduZim.Application.ZimBot.Services;

public static class ZimBotResponseParser
{
    private const string HighPrefix = "CONFIDENCE:HIGH";
    private const string LowPrefix = "CONFIDENCE:LOW";

    public static (bool IsLowConfidence, string Body) Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (true, string.Empty);

        string trimmed = raw.Trim();
        if (trimmed.StartsWith(LowPrefix, StringComparison.OrdinalIgnoreCase))
            return (true, StripPrefix(trimmed, LowPrefix));
        if (trimmed.StartsWith(HighPrefix, StringComparison.OrdinalIgnoreCase))
            return (false, StripPrefix(trimmed, HighPrefix));

        return (false, trimmed);
    }

    private static string StripPrefix(string text, string prefix)
    {
        string rest = text[prefix.Length..].TrimStart();
        return rest.Trim();
    }
}
