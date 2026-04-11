using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Billing.Commands.GenerateInvoice;

public sealed record GenerateInvoiceCommand(Guid TenantId, Guid PaymentId) : IRequest<Guid>, ITenantScopedRequest;
