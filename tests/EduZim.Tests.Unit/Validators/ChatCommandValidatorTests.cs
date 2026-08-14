using EduZim.Application.ZimBot.Commands.Chat;

namespace EduZim.Tests.Unit.Validators;

public sealed class ChatCommandValidatorTests
{
    private readonly ChatCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        ChatCommand command = new(Guid.NewGuid(), Guid.NewGuid(), "How does photosynthesis work?", null, false);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_message_fails()
    {
        ChatCommand command = new(Guid.NewGuid(), Guid.NewGuid(), "", null, false);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Empty_student_fails()
    {
        ChatCommand command = new(Guid.NewGuid(), Guid.Empty, "Hello", null, false);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Language_longer_than_32_fails()
    {
        ChatCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Hello",
            null,
            false,
            new string('x', 33));
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }
}
