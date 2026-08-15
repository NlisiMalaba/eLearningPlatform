using EduZim.Domain.Enums;

namespace EduZim.Application.Identity;

/// <summary>Font size preference updates (design property 37 / requirement 14.3).</summary>
public static class FontSizePreferenceRules
{
    public static readonly IReadOnlyList<string> AllowedValues =
        ["Small", "Medium", "Large", "ExtraLarge"];

    public static bool IsAllowed(string? value) =>
        value is "Small" or "Medium" or "Large" or "ExtraLarge";

    public static bool TryParse(string? value, out FontSize fontSize)
    {
        if (!IsAllowed(value))
        {
            fontSize = default;
            return false;
        }

        fontSize = Enum.Parse<FontSize>(value);
        return true;
    }
}
