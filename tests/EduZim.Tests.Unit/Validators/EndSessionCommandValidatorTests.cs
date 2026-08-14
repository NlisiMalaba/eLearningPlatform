using EduZim.Application.LiveClassrooms.Commands.EndSession;

namespace EduZim.Tests.Unit.Validators;

public sealed class EndSessionCommandValidatorTests
{
    private readonly EndSessionCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        EndSessionCommand command = new(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_session_fails()
    {
        EndSessionCommand command = new(Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }
}
