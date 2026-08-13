using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Validators;

public sealed class QueueNotificationCommandValidatorTests
{
    private readonly QueueNotificationCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        QueueNotificationCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationType.AssessmentDue,
            "Assessment is due tomorrow.");
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_message_fails()
    {
        QueueNotificationCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationType.AssessmentDue,
            "");
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Empty_user_fails()
    {
        QueueNotificationCommand command = new(
            Guid.NewGuid(),
            Guid.Empty,
            NotificationType.NewContent,
            "Hello");
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }
}
