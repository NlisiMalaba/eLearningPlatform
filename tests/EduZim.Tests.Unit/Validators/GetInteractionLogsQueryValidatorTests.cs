using EduZim.Application.ZimBot.Queries.GetInteractionLogs;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetInteractionLogsQueryValidatorTests
{
    private readonly GetInteractionLogsQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        GetInteractionLogsQuery query = new(Guid.NewGuid(), null);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_tenant_fails()
    {
        GetInteractionLogsQuery query = new(Guid.Empty, Guid.NewGuid());
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);
        Assert.False(result.IsValid);
    }
}
