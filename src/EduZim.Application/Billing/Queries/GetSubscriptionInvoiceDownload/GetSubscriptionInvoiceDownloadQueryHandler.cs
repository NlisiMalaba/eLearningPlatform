using EduZim.Application.Billing.Models;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Billing.Queries.GetSubscriptionInvoiceDownload;

public sealed class GetSubscriptionInvoiceDownloadQueryHandler
    : IRequestHandler<GetSubscriptionInvoiceDownloadQuery, SubscriptionInvoiceFileResult>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetSubscriptionInvoiceDownloadQueryHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SubscriptionInvoiceFileResult> Handle(
        GetSubscriptionInvoiceDownloadQuery request,
        CancellationToken cancellationToken)
    {
        TenantAccessHelper.EnsureCanManageBilling(_currentUser, request.TenantId);

        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var invoice = await _db.SubscriptionInvoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            .ConfigureAwait(false);
        if (invoice is null)
            throw new NotFoundException(nameof(SubscriptionInvoice), request.InvoiceId);

        var belongs = await _db.Subscriptions.AsNoTracking()
            .AnyAsync(
                s => s.Id == invoice.SubscriptionId && s.TenantId == request.TenantId,
                cancellationToken)
            .ConfigureAwait(false);
        if (!belongs)
            throw new NotFoundException(nameof(SubscriptionInvoice), request.InvoiceId);

        return new SubscriptionInvoiceFileResult(
            invoice.PdfContent,
            invoice.FileName,
            "application/pdf");
    }
}
