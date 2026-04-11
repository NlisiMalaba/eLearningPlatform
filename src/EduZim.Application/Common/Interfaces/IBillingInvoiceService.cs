using EduZim.Domain.Entities;

namespace EduZim.Application.Common.Interfaces;

public interface IBillingInvoiceService
{
    /// <summary>Creates a PDF invoice row for a tracked payment, or returns an existing invoice id.</summary>
    Task<Guid> CreateOrGetInvoiceAsync(Payment payment, Subscription subscription, Tenant tenant, CancellationToken cancellationToken);
}
