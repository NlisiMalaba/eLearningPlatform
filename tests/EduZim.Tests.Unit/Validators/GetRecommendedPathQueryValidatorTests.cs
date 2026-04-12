using EduZim.Application.AdaptiveLearning.Queries.GetRecommendedPath;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetRecommendedPathQueryValidatorTests
{
    private readonly GetRecommendedPathQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        var q = new GetRecommendedPathQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.True(r.IsValid);
    }
}
