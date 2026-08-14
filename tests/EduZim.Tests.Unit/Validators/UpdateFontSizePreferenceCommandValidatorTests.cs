using EduZim.Application.Identity.Commands.UpdateFontSizePreference;

namespace EduZim.Tests.Unit.Validators;

public sealed class UpdateFontSizePreferenceCommandValidatorTests
{
    private readonly UpdateFontSizePreferenceCommandValidator _validator = new();

    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    [InlineData("Large")]
    [InlineData("ExtraLarge")]
    public async Task Allowed_font_sizes_pass(string fontSize)
    {
        UpdateFontSizePreferenceCommand command = new(Guid.NewGuid(), fontSize);

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("tiny")]
    [InlineData("medium")]
    [InlineData("XL")]
    [InlineData("")]
    [InlineData("Extra-Large")]
    public async Task InvalidFontSize_IsRejected(string fontSize)
    {
        UpdateFontSizePreferenceCommand command = new(Guid.NewGuid(), fontSize);

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateFontSizePreferenceCommand.FontSize));
    }
}
