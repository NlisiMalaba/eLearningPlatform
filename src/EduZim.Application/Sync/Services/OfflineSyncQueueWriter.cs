using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Sync.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Sync.Services;

internal static class OfflineSyncQueueWriter
{
    public static async Task<int> EnqueueNewItemsAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        IReadOnlyList<OfflineSyncItemDto> items,
        CancellationToken ct)
    {
        int accepted = 0;
        foreach (OfflineSyncItemDto item in items)
        {
            if (await TryEnqueueAsync(db, tenantId, studentId, item, ct).ConfigureAwait(false))
                accepted++;
        }

        return accepted;
    }

    public static async Task<List<OfflineSyncQueue>> LoadPendingAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        CancellationToken ct)
    {
        return await db.OfflineSyncQueues
            .Where(
                q => q.TenantId == tenantId
                    && q.StudentId == studentId
                    && q.Status == SyncStatus.Pending)
            .OrderBy(q => q.LocalTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public static async Task EnsureStudentExistsAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        CancellationToken ct)
    {
        bool exists = await db.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == studentId && u.TenantId == tenantId && u.Role == UserRole.Student,
                ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(ApplicationUser), studentId);
    }

    private static async Task<bool> TryEnqueueAsync(
        IEduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        OfflineSyncItemDto item,
        CancellationToken ct)
    {
        Guid id = item.ClientId ?? Guid.NewGuid();
        bool exists = await db.OfflineSyncQueues.AnyAsync(q => q.Id == id && q.TenantId == tenantId, ct)
            .ConfigureAwait(false);
        if (exists)
            return false;

        OfflineSyncQueue row = new()
        {
            Id = id,
            TenantId = tenantId,
            StudentId = studentId,
            Payload = OfflineSyncPayloadJson.Serialize(item.Payload),
            LocalTimestamp = item.LocalTimestamp,
            Status = SyncStatus.Pending,
        };
        await db.OfflineSyncQueues.AddAsync(row, ct).ConfigureAwait(false);
        return true;
    }
}
