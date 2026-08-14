using FluentValidation;

namespace EduZim.Application.Notifications.Commands.QueueNotification;

public sealed class QueueNotificationCommandValidator : AbstractValidator<QueueNotificationCommand>
{
    public QueueNotificationCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.Message).NotEmpty().MaximumLength(4000);
    }
}
