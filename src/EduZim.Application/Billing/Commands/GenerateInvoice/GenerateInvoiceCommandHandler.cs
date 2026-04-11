using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Billing.Commands.GenerateInvoice;

public sealed class GenerateInvoiceCommandHandler : IRequestHandler<GenerateInvoiceCommand, Guid>
{
    private readonly IEduZimDbContext _db;
    private readonly IBillingInvoiceService _invoiceService;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateInvoiceCommandHandler(
        IEduZimDbContext db,
        IBillingInvoiceService invoiceService,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _invoiceService = invoiceService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
            throw new NotFoundException(nameof(Payment), request.PaymentId);

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId && s.TenantId == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
            throw new NotFoundException(nameof(Subscription), payment.SubscriptionId);

        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (tenant is null)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        var invoiceId = await _invoiceService.CreateOrGetInvoiceAsync(payment, subscription, tenant, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return invoiceId;
    }
}
