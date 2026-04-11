using FluentValidation;

namespace EduZim.Application.Billing.Queries.ListSubscriptionInvoices;

public sealed class ListSubscriptionInvoicesQueryValidator : AbstractValidator<ListSubscriptionInvoicesQuery>
{
    public ListSubscriptionInvoicesQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.SubscriptionId).NotEmpty();
    }
}
