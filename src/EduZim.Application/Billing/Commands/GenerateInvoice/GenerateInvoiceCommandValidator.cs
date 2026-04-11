using FluentValidation;

namespace EduZim.Application.Billing.Commands.GenerateInvoice;

public sealed class GenerateInvoiceCommandValidator : AbstractValidator<GenerateInvoiceCommand>
{
    public GenerateInvoiceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.PaymentId).NotEmpty();
    }
}
