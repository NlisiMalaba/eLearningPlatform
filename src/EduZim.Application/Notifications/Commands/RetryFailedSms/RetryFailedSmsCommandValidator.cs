using FluentValidation;

namespace EduZim.Application.Notifications.Commands.RetryFailedSms;

public sealed class RetryFailedSmsCommandValidator : AbstractValidator<RetryFailedSmsCommand>
{
    public RetryFailedSmsCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}
