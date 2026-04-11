using EduZim.Application.Billing.Models;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Billing.Queries.ListSubscriptionInvoices;

public sealed class ListSubscriptionInvoicesQueryHandler
    : IRequestHandler<ListSubscriptionInvoicesQuery, IReadOnlyList<SubscriptionInvoiceListItemDto>>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListSubscriptionInvoicesQueryHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SubscriptionInvoiceListItemDto>> Handle(
        ListSubscriptionInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        TenantAccessHelper.EnsureCanManageBilling(_currentUser, request.TenantId);

        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var subscriptionExists = await _db.Subscriptions.AsNoTracking()
            .AnyAsync(s => s.Id == request.SubscriptionId && s.TenantId == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!subscriptionExists)
            throw new NotFoundException(nameof(Subscription), request.SubscriptionId);

        var rows = await _db.SubscriptionInvoices.AsNoTracking()
            .Where(i => i.SubscriptionId == request.SubscriptionId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Select(i => new SubscriptionInvoiceListItemDto(
                i.Id,
                i.SubscriptionId,
                i.PaymentId,
                i.IssuedAtUtc,
                i.FileName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }
}
