namespace EduZim.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = default!;
    public string ResourceType { get; set; } = default!;
    public Guid? ResourceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}
