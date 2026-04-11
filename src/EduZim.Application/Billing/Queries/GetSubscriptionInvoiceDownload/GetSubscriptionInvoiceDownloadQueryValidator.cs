using FluentValidation;

namespace EduZim.Application.Billing.Queries.GetSubscriptionInvoiceDownload;

public sealed class GetSubscriptionInvoiceDownloadQueryValidator : AbstractValidator<GetSubscriptionInvoiceDownloadQuery>
{
    public GetSubscriptionInvoiceDownloadQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.InvoiceId).NotEmpty();
    }
}
