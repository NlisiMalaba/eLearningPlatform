using EduZim.Application.Billing.Notifications;
using EduZim.Domain.Events;
using MediatR;

namespace EduZim.Tests.Unit.Handlers;

public sealed class PaymentNotificationWiringTests
{
    [Fact]
    public void Handlers_subscribe_to_payment_succeeded_and_failed_notifications()
    {
        Assert.Contains(
            typeof(INotificationHandler<PaymentSucceededNotification>),
            typeof(PaymentSucceededNotificationHandler).GetInterfaces());
        Assert.Contains(
            typeof(INotificationHandler<PaymentFailedNotification>),
            typeof(PaymentFailedNotificationHandler).GetInterfaces());
    }
}
