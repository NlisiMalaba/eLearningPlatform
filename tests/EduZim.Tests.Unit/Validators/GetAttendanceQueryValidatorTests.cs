using EduZim.Application.LiveClassrooms.Queries.GetAttendance;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetAttendanceQueryValidatorTests
{
    private readonly GetAttendanceQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        GetAttendanceQuery query = new(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_session_fails()
    {
        GetAttendanceQuery query = new(Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);
        Assert.False(result.IsValid);
    }
}
