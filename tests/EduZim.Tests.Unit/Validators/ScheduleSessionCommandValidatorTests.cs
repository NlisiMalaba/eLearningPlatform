using EduZim.Application.LiveClassrooms.Commands.ScheduleSession;

namespace EduZim.Tests.Unit.Validators;

public sealed class ScheduleSessionCommandValidatorTests
{
    private readonly ScheduleSessionCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        ScheduleSessionCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(25),
            45);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_class_fails()
    {
        ScheduleSessionCommand command = new(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow.AddHours(25), 45);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Past_start_fails()
    {
        ScheduleSessionCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-2),
            45);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Duration_out_of_range_fails()
    {
        ScheduleSessionCommand tooShort = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(25),
            0);
        ScheduleSessionCommand tooLong = tooShort with { DurationMinutes = 481 };
        Assert.False((await _validator.ValidateAsync(tooShort)).IsValid);
        Assert.False((await _validator.ValidateAsync(tooLong)).IsValid);
    }
}
