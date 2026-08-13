using EduZim.Application.LiveClassrooms.Queries.GetJoinToken;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetJoinTokenQueryValidatorTests
{
    private readonly GetJoinTokenQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        GetJoinTokenQuery query = new(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_session_fails()
    {
        GetJoinTokenQuery query = new(Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);
        Assert.False(result.IsValid);
    }
}
