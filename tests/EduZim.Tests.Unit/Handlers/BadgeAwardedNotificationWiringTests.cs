using EduZim.Application.Gamification.Notifications;
using EduZim.Domain.Events;
using MediatR;

namespace EduZim.Tests.Unit.Handlers;

public sealed class BadgeAwardedNotificationWiringTests
{
    [Fact]
    public void Handler_subscribes_to_badge_awarded_notification()
    {
        Assert.Contains(
            typeof(INotificationHandler<BadgeAwardedNotification>),
            typeof(BadgeAwardedNotificationHandler).GetInterfaces());
    }
}
