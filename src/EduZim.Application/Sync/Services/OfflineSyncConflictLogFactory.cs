using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;

namespace EduZim.Application.Sync.Services;

internal static class OfflineSyncConflictLogFactory
{
    public static async Task AddAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid queueItemId,
        string resourceType,
        Guid resourceId,
        DateTime localTimestamp,
        DateTime serverTimestamp,
        bool localWon,
        CancellationToken ct)
    {
        DateTime utcNow = DateTime.UtcNow;
        SyncConflictLog row = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            OfflineSyncQueueId = queueItemId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            LocalTimestamp = localTimestamp,
            ServerTimestamp = serverTimestamp,
            RetainedTimestamp = SyncConflictRules.RetainedTimestamp(localTimestamp, serverTimestamp),
            LocalWon = localWon,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        await db.SyncConflictLogs.AddAsync(row, ct).ConfigureAwait(false);
    }
}
