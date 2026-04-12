using EduZim.Application.AdaptiveLearning.Commands.RefreshStudentAdaptiveCaches;

namespace EduZim.Tests.Unit.Validators;

public sealed class RefreshStudentAdaptiveCachesCommandValidatorTests
{
    private readonly RefreshStudentAdaptiveCachesCommandValidator _validator = new();

    [Fact]
    public void Accepts_valid_command()
    {
        RefreshStudentAdaptiveCachesCommand cmd = new(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult result = _validator.Validate(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_empty_tenant()
    {
        RefreshStudentAdaptiveCachesCommand cmd = new(Guid.Empty, Guid.NewGuid());
        FluentValidation.Results.ValidationResult result = _validator.Validate(cmd);
        Assert.False(result.IsValid);
    }
}
