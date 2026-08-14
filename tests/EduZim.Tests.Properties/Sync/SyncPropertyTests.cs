using EduZim.Application.Sync.Commands.ProcessOfflineQueue;
using EduZim.Application.Sync.DTOs;
using EduZim.Application.Sync.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck;
using FsCheck.Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Sync;

/// <summary>Feature: elearning-app-zimbabwe — Sync property 33.</summary>
public sealed class SyncPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 33: Offline Sync Conflict Resolution by Timestamp — Validates: Requirements 12.5
    [Property(MaxTest = 100)]
    public async Task Property33_later_timestamp_is_retained_and_conflict_is_logged(
        int offsetMinutesRaw,
        NonNegativeInt serverTimeRaw,
        NonNegativeInt localTimeRaw,
        bool serverCompleted,
        bool localCompleted)
    {
        int offsetMinutes = ((offsetMinutesRaw % 241) + 241) % 241 - 120;
        int serverTime = serverTimeRaw.Get % 1000;
        int localTime = localTimeRaw.Get % 1000;
        DateTime serverTs = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc);
        DateTime localTs = serverTs.AddMinutes(offsetMinutes);

        using ServiceProvider provider = SyncPropertyTestHost.Create();
        using IServiceScope scope = SyncPropertyTestHost.CreateScope(provider);
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        MutableCurrentUser current = provider.GetRequiredService<MutableCurrentUser>();

        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        Guid queueId = Guid.NewGuid();
        await SyncPropertySeeds
            .SeedConflictScenarioAsync(db, tenantId, studentId, moduleId, serverTs, serverTime, serverCompleted)
            .ConfigureAwait(false);

        current.UserId = studentId;
        current.TenantId = tenantId;
        current.Role = UserRole.Student;
        await mediator
            .Send(
                new ProcessOfflineQueueCommand(
                    tenantId,
                    studentId,
                    [
                        new OfflineSyncItemDto(
                            queueId,
                            localTs,
                            new OfflineSyncPayloadDto(
                                OfflineSyncKinds.ModuleProgress,
                                ModuleId: moduleId,
                                IsCompleted: localCompleted,
                                CompletedAt: localCompleted ? localTs : null,
                                TimeOnTaskSeconds: localTime)),
                    ]),
                CancellationToken.None)
            .ConfigureAwait(false);

        await AssertLastWriteWinsAsync(
                db,
                tenantId,
                studentId,
                moduleId,
                queueId,
                localTs,
                serverTs,
                localTime,
                serverTime,
                localCompleted,
                serverCompleted)
            .ConfigureAwait(false);
    }

    private static async Task AssertLastWriteWinsAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        Guid queueId,
        DateTime localTs,
        DateTime serverTs,
        int localTime,
        int serverTime,
        bool localCompleted,
        bool serverCompleted)
    {
        bool localWins = OracleLocalWins(localTs, serverTs);
        StudentProgress progress = await db.StudentProgresses.AsNoTracking()
            .SingleAsync(p => p.TenantId == tenantId && p.StudentId == studentId && p.ModuleId == moduleId)
            .ConfigureAwait(false);
        Assert.Equal(localWins ? localTime : serverTime, progress.TimeOnTaskSeconds);
        Assert.Equal(localWins ? localCompleted : serverCompleted, progress.IsCompleted);

        OfflineSyncQueue queue = await db.OfflineSyncQueues.AsNoTracking()
            .SingleAsync(q => q.Id == queueId)
            .ConfigureAwait(false);
        Assert.Equal(SyncStatus.Conflicted, queue.Status);

        SyncConflictLog log = await db.SyncConflictLogs.AsNoTracking()
            .SingleAsync(l => l.TenantId == tenantId && l.OfflineSyncQueueId == queueId)
            .ConfigureAwait(false);
        Assert.Equal(studentId, log.StudentId);
        Assert.Equal(OfflineSyncKinds.ModuleProgress, log.ResourceType);
        Assert.Equal(localTs, log.LocalTimestamp);
        Assert.Equal(serverTs, log.ServerTimestamp);
        Assert.Equal(localWins ? localTs : serverTs, log.RetainedTimestamp);
        Assert.Equal(localWins, log.LocalWon);
    }

    private static bool OracleLocalWins(DateTime localTimestamp, DateTime serverTimestamp) =>
        localTimestamp > serverTimestamp;
}
