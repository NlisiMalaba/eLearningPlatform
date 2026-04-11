namespace EduZim.Domain.Entities;

/// <summary>
/// Time-limited, tenant-scoped code used to link a parent account to a student (School Tier).
/// </summary>
public class TenantInviteCode
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentUserId { get; set; }
    public string Code { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public Guid? UsedByParentUserId { get; set; }
}
