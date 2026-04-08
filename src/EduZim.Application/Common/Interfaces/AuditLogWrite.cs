namespace EduZim.Application.Common.Interfaces;

public sealed record AuditLogWrite(
    Guid? TenantId,
    Guid UserId,
    string Action,
    string ResourceType,
    Guid? ResourceId,
    string? IpAddress);
