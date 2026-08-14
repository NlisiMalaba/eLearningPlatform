using FluentValidation;

namespace EduZim.Application.Identity.Commands.UpdateFontSizePreference;

public sealed class UpdateFontSizePreferenceCommandValidator
    : AbstractValidator<UpdateFontSizePreferenceCommand>
{
    public UpdateFontSizePreferenceCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.FontSize)
            .NotEmpty()
            .Must(FontSizePreferenceRules.IsAllowed)
            .WithMessage("Font size must be Small, Medium, Large, or ExtraLarge.");
    }
}
