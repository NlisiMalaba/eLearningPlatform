using EduZim.Application.Notifications.Commands.UpdateNotificationPreferences;
using EduZim.Application.Notifications.DTOs;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Validators;

public sealed class UpdateNotificationPreferencesCommandValidatorTests
{
    private readonly UpdateNotificationPreferencesCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        UpdateNotificationPreferencesCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new NotificationPreferenceItemDto(NotificationType.AssessmentDue, true, true, false)]);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_preferences_fails()
    {
        UpdateNotificationPreferencesCommand command = new(Guid.NewGuid(), Guid.NewGuid(), []);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Duplicate_types_fail()
    {
        UpdateNotificationPreferencesCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new NotificationPreferenceItemDto(NotificationType.AssessmentDue, true, false, false),
                new NotificationPreferenceItemDto(NotificationType.AssessmentDue, false, true, false),
            ]);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }
}
