namespace EduZim.Domain.Exceptions;

/// <summary>
/// Thrown when an operation targets a tenant or resource the caller is not allowed to access (defence in depth with RLS).
/// </summary>
public class TenantAccessViolationException : DomainException
{
    public Guid? TenantId { get; }
    public Guid? ResourceId { get; }

    public TenantAccessViolationException()
        : base("Access to this tenant or resource is denied.")
    {
    }

    public TenantAccessViolationException(string message)
        : base(message)
    {
    }

    public TenantAccessViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TenantAccessViolationException(string message, Guid? tenantId, Guid? resourceId = null)
        : base(message)
    {
        TenantId = tenantId;
        ResourceId = resourceId;
    }
}
