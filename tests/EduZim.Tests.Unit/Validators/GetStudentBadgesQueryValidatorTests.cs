using EduZim.Application.Gamification.Queries.GetStudentBadges;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetStudentBadgesQueryValidatorTests
{
    private readonly GetStudentBadgesQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        var q = new GetStudentBadgesQuery(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.True(r.IsValid);
    }

    [Fact]
    public async Task Empty_student_fails()
    {
        var q = new GetStudentBadgesQuery(Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.False(r.IsValid);
    }
}
