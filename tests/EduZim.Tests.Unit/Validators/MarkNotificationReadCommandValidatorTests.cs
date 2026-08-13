using EduZim.Application.Notifications.Commands.MarkNotificationRead;

namespace EduZim.Tests.Unit.Validators;

public sealed class MarkNotificationReadCommandValidatorTests
{
    private readonly MarkNotificationReadCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        MarkNotificationReadCommand command = new(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_notification_id_fails()
    {
        MarkNotificationReadCommand command = new(Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }
}
