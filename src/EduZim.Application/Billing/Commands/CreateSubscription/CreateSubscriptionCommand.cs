using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Billing.Commands.CreateSubscription;

public sealed record CreateSubscriptionCommand(Guid TenantId, BillingCycle Cycle, int StudentCount) : IRequest<Guid>, ITenantScopedRequest;
