using EduZim.Application.Identity;
using EduZim.Application.Identity.Commands.UpdateFontSizePreference;
using FsCheck;
using FsCheck.Xunit;

namespace EduZim.Tests.Properties.Identity;

/// <summary>Feature: elearning-app-zimbabwe — Identity font size preference (property 37).</summary>
public sealed class FontSizePreferencePropertyTests
{
    private readonly UpdateFontSizePreferenceCommandValidator _validator = new();

    // Feature: elearning-app-zimbabwe, Property 37: Font Size Preference Validation — Validates: Requirements 14.3
    [Property(MaxTest = 100)]
    public void Property37_only_small_medium_large_extralarge_are_accepted(
        bool useAllowed,
        NonNegativeInt index,
        string noise)
    {
        string fontSize = useAllowed
            ? FontSizePreferenceRules.AllowedValues[index.Get % FontSizePreferenceRules.AllowedValues.Count]
            : ToInvalidFontSize(noise, index.Get);

        FluentValidation.Results.ValidationResult result = _validator.Validate(
            new UpdateFontSizePreferenceCommand(Guid.NewGuid(), fontSize));

        Assert.Equal(useAllowed, result.IsValid);
        Assert.Equal(useAllowed, FontSizePreferenceRules.IsAllowed(fontSize));
        if (!useAllowed)
        {
            Assert.Contains(
                result.Errors,
                e => e.PropertyName == nameof(UpdateFontSizePreferenceCommand.FontSize));
        }
    }

    private static string ToInvalidFontSize(string noise, int index)
    {
        if (FontSizePreferenceRules.IsAllowed(noise))
            return noise + "-rejected";

        if (string.IsNullOrWhiteSpace(noise))
            return $"not-a-size-{index}";

        return noise;
    }
}
