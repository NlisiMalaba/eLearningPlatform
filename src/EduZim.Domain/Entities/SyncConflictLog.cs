namespace EduZim.Domain.Entities;

public class SyncConflictLog : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid OfflineSyncQueueId { get; set; }
    public string ResourceType { get; set; } = default!;
    public Guid ResourceId { get; set; }
    public DateTime LocalTimestamp { get; set; }
    public DateTime ServerTimestamp { get; set; }
    public DateTime RetainedTimestamp { get; set; }
    public bool LocalWon { get; set; }
}
