using EduZim.Application.AdaptiveLearning.Notifications;
using EduZim.Application.Assessments.Notifications;
using EduZim.Application.Gamification.Notifications;
using EduZim.Domain.Events;
using MediatR;

namespace EduZim.Tests.Unit.Handlers;

public sealed class AssessmentSubmittedNotificationWiringTests
{
    [Fact]
    public void Required_handlers_subscribe_to_assessment_submitted_notification()
    {
        Type contract = typeof(INotificationHandler<AssessmentSubmittedNotification>);
        Type[] handlers =
        [
            typeof(UpdateLearningProfileCommandHandler),
            typeof(AwardPointsCommandHandler),
            typeof(CheckAndAwardBadgesCommandHandler),
            typeof(AssessmentSubmittedNotificationHandler),
        ];

        foreach (Type handler in handlers)
            Assert.Contains(contract, handler.GetInterfaces());
    }
}
