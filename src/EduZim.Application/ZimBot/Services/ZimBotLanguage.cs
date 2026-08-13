namespace EduZim.Application.ZimBot.Services;

public static class ZimBotLanguage
{
    public const string English = "English";
    public const string Shona = "Shona";
    public const string Ndebele = "Ndebele";
    public const string Kalanga = "Kalanga";

    public static string Resolve(string? preferredLanguage)
    {
        if (string.IsNullOrWhiteSpace(preferredLanguage))
            return English;

        string key = preferredLanguage.Trim().ToLowerInvariant();
        return key switch
        {
            "en" or "eng" or "english" => English,
            "sn" or "sna" or "shona" => Shona,
            "nd" or "nde" or "ndc" or "nr" or "ndebele" => Ndebele,
            "kck" or "kalanga" => Kalanga,
            _ => Truncate(preferredLanguage.Trim()),
        };
    }

    private static string Truncate(string value) =>
        value.Length <= 32 ? value : value[..32];
}
