using EduZim.Application.Notifications.Commands.PublishInactiveStudentAlerts;
using MediatR;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Daily Hangfire entry: notify parents of students inactive for 7 days (requirement 10.6).</summary>
public sealed class StudentInactivityAlertJob
{
    private readonly IMediator _mediator;

    public StudentInactivityAlertJob(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task RunAsync(CancellationToken cancellationToken = default) =>
        _mediator.Send(new PublishInactiveStudentAlertsCommand(), cancellationToken);
}
