using FluentValidation;

namespace EduZim.Application.Billing.Commands.HandlePaymentSucceeded;

public sealed class HandlePaymentSucceededCommandValidator : AbstractValidator<HandlePaymentSucceededCommand>
{
    public HandlePaymentSucceededCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SubscriptionId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).NotEmpty().MaximumLength(8);
    }
}
