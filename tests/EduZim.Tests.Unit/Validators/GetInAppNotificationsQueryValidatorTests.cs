using EduZim.Application.Notifications.Queries.GetInAppNotifications;

namespace EduZim.Tests.Unit.Validators;

public sealed class GetInAppNotificationsQueryValidatorTests
{
    private readonly GetInAppNotificationsQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes()
    {
        GetInAppNotificationsQuery query = new(Guid.NewGuid(), Guid.NewGuid());
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_user_fails()
    {
        GetInAppNotificationsQuery query = new(Guid.NewGuid(), Guid.Empty);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(query);
        Assert.False(result.IsValid);
    }
}
