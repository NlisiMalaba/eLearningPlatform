using EduZim.Application.Common.Models;

namespace EduZim.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<PaymentResult> CreateSubscriptionAsync(CreatePaymentRequest request, CancellationToken ct = default);
    Task<Invoice> GenerateInvoiceAsync(Guid subscriptionId, Guid paymentId, CancellationToken ct = default);
}
