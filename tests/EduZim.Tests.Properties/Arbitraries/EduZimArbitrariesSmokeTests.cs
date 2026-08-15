using EduZim.Domain.Entities;
using FsCheck.Xunit;

namespace EduZim.Tests.Properties.Arbitraries;

public sealed class EduZimArbitrariesSmokeTests
{
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(EduZimArbitraries) })]
    public void Generated_domain_values_are_well_formed(
        ApplicationUser user,
        Tenant tenant,
        AssessmentAttempt attempt,
        Subscription subscription,
        ContentItem content,
        Notification notification,
        OfflineProgressItem offline)
    {
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.False(string.IsNullOrWhiteSpace(user.Email));
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.False(string.IsNullOrWhiteSpace(tenant.Name));
        Assert.InRange(attempt.ScorePercent, 0, 100);
        Assert.True(subscription.CurrentPeriodEnd > subscription.CurrentPeriodStart);
        Assert.False(string.IsNullOrWhiteSpace(content.StorageKey));
        Assert.InRange(notification.RetryCount, 0, 3);
        Assert.True(offline.TimeOnTaskSeconds >= 0);
        Assert.NotEqual(Guid.Empty, offline.ModuleId);
    }
}
