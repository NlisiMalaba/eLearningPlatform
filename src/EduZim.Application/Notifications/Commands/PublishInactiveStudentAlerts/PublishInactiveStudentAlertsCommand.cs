using MediatR;

namespace EduZim.Application.Notifications.Commands.PublishInactiveStudentAlerts;

public sealed record PublishInactiveStudentAlertsCommand : IRequest<Unit>;
