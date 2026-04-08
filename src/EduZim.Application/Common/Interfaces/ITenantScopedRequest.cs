namespace EduZim.Application.Common.Interfaces;

/// <summary>
/// Marks a MediatR request that carries an explicit tenant scope; <c>TenantScopeBehaviour</c> enforces it matches the caller.
/// </summary>
public interface ITenantScopedRequest
{
    Guid TenantId { get; }
}
