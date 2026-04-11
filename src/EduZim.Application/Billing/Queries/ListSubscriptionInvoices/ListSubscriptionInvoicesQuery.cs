using EduZim.Application.Billing.Models;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Billing.Queries.ListSubscriptionInvoices;

public sealed record ListSubscriptionInvoicesQuery(Guid TenantId, Guid SubscriptionId)
    : IRequest<IReadOnlyList<SubscriptionInvoiceListItemDto>>, ITenantScopedRequest;
