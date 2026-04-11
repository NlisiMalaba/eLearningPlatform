using EduZim.Application.Billing.Models;
using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Billing.Queries.GetSubscriptionInvoiceDownload;

public sealed record GetSubscriptionInvoiceDownloadQuery(Guid TenantId, Guid InvoiceId)
    : IRequest<SubscriptionInvoiceFileResult>, ITenantScopedRequest;
