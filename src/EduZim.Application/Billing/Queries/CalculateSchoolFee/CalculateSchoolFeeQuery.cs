using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Billing.Queries.CalculateSchoolFee;

public sealed record CalculateSchoolFeeQuery(Guid TenantId, BillingCycle Cycle, int StudentCount) : IRequest<decimal>, ITenantScopedRequest;
