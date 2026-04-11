using FluentValidation;

namespace EduZim.Application.Billing.Commands.HandlePaymentFailed;

public sealed class HandlePaymentFailedCommandValidator : AbstractValidator<HandlePaymentFailedCommand>
{
    public HandlePaymentFailedCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SubscriptionId).NotEmpty();
    }
}
