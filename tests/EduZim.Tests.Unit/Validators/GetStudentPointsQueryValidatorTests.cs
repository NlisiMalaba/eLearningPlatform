using EduZim.Application.Gamification.Queries.GetStudentPoints;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetStudentPointsQueryValidatorTests
{
    private readonly GetStudentPointsQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        var q = new GetStudentPointsQuery(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.True(r.IsValid);
    }

    [Fact]
    public async Task Empty_student_fails()
    {
        var q = new GetStudentPointsQuery(Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.False(r.IsValid);
    }
}
