using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.Marketplace.Commands.ApproveAccess;

public sealed record ApproveAccessCommand(Guid TenantId, Guid ContentPackId, Guid RequestingTenantId)
    : IRequest<Unit>, ITenantScopedRequest;
