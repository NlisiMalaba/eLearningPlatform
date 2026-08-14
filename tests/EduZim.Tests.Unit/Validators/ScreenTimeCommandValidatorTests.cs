using EduZim.Application.Progress.Commands.RecordStudentSessionHeartbeat;
using EduZim.Application.Progress.Commands.SetDailyScreenTimeLimit;
using EduZim.Application.Progress.Commands.StartStudentSession;

namespace EduZim.Tests.Unit.Validators;

public sealed class ScreenTimeCommandValidatorTests
{
    [Fact]
    public async Task Set_limit_valid_command_passes()
    {
        var validator = new SetDailyScreenTimeLimitCommandValidator();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new SetDailyScreenTimeLimitCommand(Guid.NewGuid(), Guid.NewGuid(), 3_600));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Set_limit_null_clears_limit()
    {
        var validator = new SetDailyScreenTimeLimitCommandValidator();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new SetDailyScreenTimeLimitCommand(Guid.NewGuid(), Guid.NewGuid(), null));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Set_limit_out_of_range_fails()
    {
        var validator = new SetDailyScreenTimeLimitCommandValidator();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new SetDailyScreenTimeLimitCommand(Guid.NewGuid(), Guid.NewGuid(), 90_000));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Start_session_empty_student_fails()
    {
        var validator = new StartStudentSessionCommandValidator();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new StartStudentSessionCommand(Guid.NewGuid(), Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Heartbeat_valid_command_passes()
    {
        var validator = new RecordStudentSessionHeartbeatCommandValidator();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new RecordStudentSessionHeartbeatCommand(Guid.NewGuid(), Guid.NewGuid()));
        Assert.True(result.IsValid);
    }
}
