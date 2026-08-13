using EduZim.Application.Notifications.Commands.PublishInactiveStudentAlerts;

namespace EduZim.Tests.Unit.Validators;

public sealed class PublishInactiveStudentAlertsCommandValidatorTests
{
    private readonly PublishInactiveStudentAlertsCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        FluentValidation.Results.ValidationResult result =
            await _validator.ValidateAsync(new PublishInactiveStudentAlertsCommand());
        Assert.True(result.IsValid);
    }
}
