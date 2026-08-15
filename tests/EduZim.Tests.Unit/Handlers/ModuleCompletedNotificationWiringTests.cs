using EduZim.Application.AdaptiveLearning.Notifications;
using EduZim.Application.Gamification.Notifications;
using EduZim.Application.Progress.Notifications;
using EduZim.Domain.Events;
using MediatR;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ModuleCompletedNotificationWiringTests
{
    [Fact]
    public void Required_handlers_subscribe_to_module_completed_notification()
    {
        Type contract = typeof(INotificationHandler<ModuleCompletedNotification>);
        Type[] handlers =
        [
            typeof(RecordModuleCompletionCommandHandler),
            typeof(AwardPointsCommandHandler),
            typeof(CheckAndAwardBadgesCommandHandler),
            typeof(UpdateLearningProfileCommandHandler),
        ];

        foreach (Type handler in handlers)
            Assert.Contains(contract, handler.GetInterfaces());
    }
}
