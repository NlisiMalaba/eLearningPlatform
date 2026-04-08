using EduZim.Domain.Enums;

namespace EduZim.Domain.Entities;

public class OfflineSyncQueue
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public string Payload { get; set; } = default!;
    public DateTime LocalTimestamp { get; set; }
    public SyncStatus Status { get; set; }
}
