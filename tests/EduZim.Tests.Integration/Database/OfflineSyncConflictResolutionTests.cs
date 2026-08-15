using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EduZim.API.Contracts;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Integration.Database;

[Collection(PostgresRlsCollection.Name)]
public sealed class OfflineSyncConflictResolutionTests
{
    private readonly PostgresRlsFixture _fixture;

    public OfflineSyncConflictResolutionTests(PostgresRlsFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Later_local_progress_wins_and_writes_conflict_log()
    {
        _fixture.EnsureDockerAvailable();

        DateTime serverTs = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-2), DateTimeKind.Utc);
        DateTime localTs = serverTs.AddMinutes(30);
        await AssertConflictResolutionAsync(
            serverTs,
            localTs,
            serverTimeOnTask: 120,
            localTimeOnTask: 480,
            serverCompleted: false,
            localCompleted: true,
            expectLocalWon: true);
    }

    [SkippableFact]
    public async Task Later_server_progress_wins_and_writes_conflict_log()
    {
        _fixture.EnsureDockerAvailable();

        DateTime localTs = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-3), DateTimeKind.Utc);
        DateTime serverTs = localTs.AddMinutes(45);
        await AssertConflictResolutionAsync(
            serverTs,
            localTs,
            serverTimeOnTask: 900,
            localTimeOnTask: 60,
            serverCompleted: true,
            localCompleted: false,
            expectLocalWon: false);
    }

    private async Task AssertConflictResolutionAsync(
        DateTime serverTs,
        DateTime localTs,
        int serverTimeOnTask,
        int localTimeOnTask,
        bool serverCompleted,
        bool localCompleted,
        bool expectLocalWon)
    {
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        Guid queueId = Guid.NewGuid();
        ApplicationUser student = await SeedServerProgressAsync(
            studentId,
            moduleId,
            serverTs,
            serverTimeOnTask,
            serverCompleted);

        using HttpClient client = _fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IssueToken(student));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1.0/sync/upload?tenantId={_fixture.TenantAId}",
            new UploadOfflineQueueRequest
            {
                StudentId = studentId,
                Items =
                [
                    new UploadOfflineQueueItemRequest
                    {
                        ClientId = queueId,
                        LocalTimestamp = localTs,
                        Payload = new UploadOfflineQueuePayloadRequest
                        {
                            Kind = OfflineSyncKinds.ModuleProgress,
                            ModuleId = moduleId,
                            IsCompleted = localCompleted,
                            CompletedAt = localCompleted ? localTs : null,
                            TimeOnTaskSeconds = localTimeOnTask,
                        },
                    },
                ],
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UploadOfflineQueueResponse? body =
            await response.Content.ReadFromJsonAsync<UploadOfflineQueueResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.AcceptedCount);
        Assert.Equal(0, body.SyncedCount);
        Assert.Equal(1, body.ConflictedCount);

        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await db.SetSessionTenantIdAsync(_fixture.TenantAId);

        StudentProgress progress = await db.StudentProgresses.AsNoTracking()
            .SingleAsync(p => p.StudentId == studentId && p.ModuleId == moduleId);
        Assert.Equal(expectLocalWon ? localTimeOnTask : serverTimeOnTask, progress.TimeOnTaskSeconds);
        Assert.Equal(expectLocalWon ? localCompleted : serverCompleted, progress.IsCompleted);

        OfflineSyncQueue queue = await db.OfflineSyncQueues.AsNoTracking()
            .SingleAsync(q => q.Id == queueId);
        Assert.Equal(SyncStatus.Conflicted, queue.Status);

        SyncConflictLog log = await db.SyncConflictLogs.AsNoTracking()
            .SingleAsync(l => l.OfflineSyncQueueId == queueId);
        Assert.Equal(studentId, log.StudentId);
        Assert.Equal(_fixture.TenantAId, log.TenantId);
        Assert.Equal(OfflineSyncKinds.ModuleProgress, log.ResourceType);
        Assert.Equal(moduleId, log.ResourceId);
        Assert.Equal(expectLocalWon, log.LocalWon);
        Assert.Equal(expectLocalWon ? localTs : serverTs, log.RetainedTimestamp, TimeSpan.FromSeconds(1));
        Assert.Equal(localTs, log.LocalTimestamp, TimeSpan.FromSeconds(1));
        Assert.Equal(serverTs, log.ServerTimestamp, TimeSpan.FromSeconds(1));
    }

    private async Task<ApplicationUser> SeedServerProgressAsync(
        Guid studentId,
        Guid moduleId,
        DateTime serverTs,
        int serverTimeOnTask,
        bool serverCompleted)
    {
        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await db.SetSessionTenantIdAsync(_fixture.TenantAId);

        string email = $"sync-{studentId:N}@eduzim.test";
        ApplicationUser student = new()
        {
            Id = studentId,
            TenantId = _fixture.TenantAId,
            Role = UserRole.Student,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Sync Student",
            NormalizedUserName = email.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        db.Users.Add(student);
        db.Modules.Add(new Module
        {
            Id = moduleId,
            TenantId = _fixture.TenantAId,
            Title = "Offline conflict module",
            Subject = "Maths",
            Grade = GradeLevel.Grade1,
            SequenceOrder = 1,
            CreatedAt = serverTs,
            UpdatedAt = serverTs,
        });
        db.StudentProgresses.Add(new StudentProgress
        {
            Id = Guid.NewGuid(),
            TenantId = _fixture.TenantAId,
            StudentId = studentId,
            ModuleId = moduleId,
            IsCompleted = serverCompleted,
            CompletedAt = serverCompleted ? serverTs : null,
            TimeOnTaskSeconds = serverTimeOnTask,
            CreatedAt = serverTs,
            UpdatedAt = serverTs,
        });
        await db.SaveChangesAsync();
        return student;
    }

    private string IssueToken(ApplicationUser student)
    {
        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        IAccessTokenIssuer issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();
        return issuer.IssueForUser(student).Token;
    }
}
