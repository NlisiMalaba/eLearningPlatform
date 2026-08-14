using EduZim.Application.Notifications.DTOs;
using FluentValidation;

namespace EduZim.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandValidator
    : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Preferences).NotNull().NotEmpty();
        RuleForEach(c => c.Preferences).ChildRules(item =>
        {
            item.RuleFor(x => x.Type).IsInEnum();
        });
        RuleFor(c => c.Preferences)
            .Must(HaveUniqueTypes)
            .When(c => c.Preferences is not null)
            .WithMessage("Duplicate notification types are not allowed.");
    }

    private static bool HaveUniqueTypes(IReadOnlyList<NotificationPreferenceItemDto> preferences)
    {
        return preferences.Select(p => p.Type).Distinct().Count() == preferences.Count;
    }
}
