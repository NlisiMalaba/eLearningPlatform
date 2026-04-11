using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;
using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Billing.Services;

public sealed class BillingInvoiceService : IBillingInvoiceService
{
    private readonly IEduZimDbContext _db;
    private readonly IBillingPricingService _pricing;
    private readonly IInvoicePdfGenerator _pdfGenerator;
    private readonly ILogger<BillingInvoiceService> _logger;

    public BillingInvoiceService(
        IEduZimDbContext db,
        IBillingPricingService pricing,
        IInvoicePdfGenerator pdfGenerator,
        ILogger<BillingInvoiceService> logger)
    {
        _db = db;
        _pricing = pricing;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    public async Task<Guid> CreateOrGetInvoiceAsync(
        Payment payment,
        Subscription subscription,
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        var existing = await _db.SubscriptionInvoices.AsNoTracking()
            .Where(i => i.PaymentId == payment.Id)
            .Select(i => i.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing != Guid.Empty)
            return existing;

        var unitPrice = _pricing.GetUnitPrice(tenant.Tier, subscription.Cycle);
        var studentCount = Math.Max(1, subscription.StudentCount ?? 1);

        var invoiceId = Guid.NewGuid();
        var issuedAt = payment.CreatedAtUtc;
        var model = new InvoicePdfModel
        {
            InvoiceId = invoiceId,
            SubscriptionId = subscription.Id,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            IssuedAtUtc = issuedAt,
            Cycle = subscription.Cycle,
            StudentCount = studentCount,
            UnitPrice = unitPrice,
            TotalAmount = payment.Amount,
            Currency = payment.Currency,
            PaymentProviderReference = payment.ProviderReference,
        };

        var pdf = await _pdfGenerator.GeneratePdfAsync(model, cancellationToken).ConfigureAwait(false);
        var invoice = new SubscriptionInvoice
        {
            Id = invoiceId,
            SubscriptionId = subscription.Id,
            PaymentId = payment.Id,
            IssuedAtUtc = issuedAt,
            PdfContent = pdf,
            FileName = $"invoice-{invoiceId:N}.pdf",
        };

        await _db.SubscriptionInvoices.AddAsync(invoice, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Created subscription invoice {InvoiceId} for payment {PaymentId}, subscription {SubscriptionId}.",
            invoiceId,
            payment.Id,
            subscription.Id);

        return invoiceId;
    }
}
