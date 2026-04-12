using EduZim.Application.AdaptiveLearning.Queries.GetWeeklySummary;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetWeeklySummaryQueryValidatorTests
{
    private readonly GetWeeklySummaryQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        var q = new GetWeeklySummaryQuery(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.True(r.IsValid);
    }

    [Fact]
    public async Task Empty_student_fails()
    {
        var q = new GetWeeklySummaryQuery(Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.False(r.IsValid);
    }
}
