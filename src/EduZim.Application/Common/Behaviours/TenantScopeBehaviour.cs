using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;

namespace EduZim.Application.Common.Behaviours;

public sealed class TenantScopeBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUser _currentUser;

    public TenantScopeBehaviour(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITenantScopedRequest tenantRequest)
            return await next();

        if (_currentUser.Role == UserRole.PlatformAdmin)
            return await next();

        if (_currentUser.TenantId is null)
        {
            throw new TenantAccessViolationException(
                "Tenant context is required for this request.",
                tenantRequest.TenantId,
                null);
        }

        if (_currentUser.TenantId.Value != tenantRequest.TenantId)
        {
            throw new TenantAccessViolationException(
                "The requested tenant does not match the authenticated tenant.",
                tenantRequest.TenantId,
                null);
        }

        return await next();
    }
}
