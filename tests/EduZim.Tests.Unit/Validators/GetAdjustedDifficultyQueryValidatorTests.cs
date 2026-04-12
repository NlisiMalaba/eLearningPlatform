using EduZim.Application.AdaptiveLearning.Queries.GetAdjustedDifficulty;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetAdjustedDifficultyQueryValidatorTests
{
    private readonly GetAdjustedDifficultyQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        var q = new GetAdjustedDifficultyQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.True(r.IsValid);
    }

    [Fact]
    public async Task Empty_module_fails()
    {
        var q = new GetAdjustedDifficultyQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.False(r.IsValid);
    }
}
