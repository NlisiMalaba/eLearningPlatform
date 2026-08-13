using EduZim.Application.Notifications.Commands.RetryFailedSms;
using Hangfire;
using MediatR;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Hangfire entry: retry a failed SMS notification (requirement 15.5).</summary>
public sealed class RetryFailedSmsJob
{
    private readonly IMediator _mediator;

    public RetryFailedSmsJob(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AutomaticRetry(Attempts = 0)]
    public Task RunAsync(Guid tenantId, Guid notificationId) =>
        _mediator.Send(new RetryFailedSmsCommand(tenantId, notificationId), CancellationToken.None);
}
