using EduZim.Application.Gamification.Queries.GetLeaderboard;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetLeaderboardQueryValidatorTests
{
    private readonly GetLeaderboardQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        var q = new GetLeaderboardQuery(Guid.NewGuid(), Limit: 20);
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.True(r.IsValid);
    }

    [Fact]
    public async Task Empty_tenant_fails()
    {
        var q = new GetLeaderboardQuery(Guid.Empty);
        FluentValidation.Results.ValidationResult r = await _validator.ValidateAsync(q);
        Assert.False(r.IsValid);
    }

    [Fact]
    public async Task Limit_outside_range_fails()
    {
        var tooLow = new GetLeaderboardQuery(Guid.NewGuid(), Limit: 0);
        var tooHigh = new GetLeaderboardQuery(Guid.NewGuid(), Limit: 101);
        Assert.False((await _validator.ValidateAsync(tooLow)).IsValid);
        Assert.False((await _validator.ValidateAsync(tooHigh)).IsValid);
    }
}
