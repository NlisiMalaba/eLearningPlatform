using EduZim.Application.Exceptions;
using EduZim.Application.Progress.Commands.RecordStudentSessionHeartbeat;
using EduZim.Application.Progress.Commands.StartStudentSession;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Queries.GetParentDashboard;
using EduZim.Application.Progress.Queries.GetStudentProgress;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck.Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Progress;

/// <summary>Feature: elearning-app-zimbabwe — Progress properties 12, 13, 14, 29, 31.</summary>
public sealed class ProgressPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 12: Session Inactivity Pause — Validates: Requirements 3.7
    [Property(MaxTest = 100)]
    public async Task Property12_active_toddler_session_pauses_after_sixty_seconds_without_interaction(
        byte idleExtraRaw,
        byte accumulatedRaw)
    {
        int idleSeconds = PreschoolSessionRules.InactivityPauseAfterSeconds + (idleExtraRaw % 120);
        using ServiceProvider provider = ProgressPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        (EduZimDbContext db, IMediator mediator, _, MutableCurrentUser current) =
            ProgressPropertyFlow.Resolve(provider, scope);

        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        await ProgressPropertySeeds.SeedTenantAsync(db, tenantId, TenantTier.PreSchool).ConfigureAwait(false);
        await ProgressPropertySeeds.SeedStudentAsync(db, tenantId, studentId).ConfigureAwait(false);
        await ProgressPropertySeeds
            .SeedSessionAsync(
                db,
                tenantId,
                studentId,
                SessionStatus.Active,
                today,
                accumulatedSeconds: accumulatedRaw,
                lastHeartbeatAt: utcNow,
                lastInteractionAt: utcNow.AddSeconds(-idleSeconds))
            .ConfigureAwait(false);

        ProgressPropertyFlow.AsStudent(current, tenantId, studentId);
        StudentSessionDto dto = await mediator
            .Send(new RecordStudentSessionHeartbeatCommand(tenantId, studentId), CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(SessionStatus.Paused, dto.Status);
        Assert.False(dto.LimitReached);
        StudentSession row = await db.StudentSessions.SingleAsync().ConfigureAwait(false);
        Assert.Equal(SessionStatus.Paused, row.Status);
    }

    // Feature: elearning-app-zimbabwe, Property 13: Module Completion Unlocks Next Module — Validates: Requirements 4.4
    [Property(MaxTest = 100)]
    public async Task Property13_completing_module_unlocks_next_in_sequence(byte lengthRaw, byte indexRaw)
    {
        int length = (lengthRaw % 5) + 2;
        int completeIndex = indexRaw % (length - 1);
        using ServiceProvider provider = ProgressPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        (EduZimDbContext db, IMediator mediator, IPublisher publisher, MutableCurrentUser current) =
            ProgressPropertyFlow.Resolve(provider, scope);

        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        await ProgressPropertySeeds.SeedStudentAsync(db, tenantId, studentId).ConfigureAwait(false);
        List<Guid> moduleIds = await ProgressPropertySeeds
            .SeedModuleSequenceAsync(db, tenantId, "Math", length)
            .ConfigureAwait(false);

        Guid completedId = moduleIds[completeIndex];
        Guid nextId = moduleIds[completeIndex + 1];
        await publisher
            .Publish(new ModuleCompletedNotification(studentId, completedId, tenantId), CancellationToken.None)
            .ConfigureAwait(false);

        ProgressPropertyFlow.AsTeacher(current, tenantId);
        StudentProgressDto dto = await mediator
            .Send(new GetStudentProgressQuery(tenantId, studentId), CancellationToken.None)
            .ConfigureAwait(false);

        ModuleProgressDto completed = dto.Subjects.SelectMany(s => s.Modules).Single(m => m.ModuleId == completedId);
        ModuleProgressDto next = dto.Subjects.SelectMany(s => s.Modules).Single(m => m.ModuleId == nextId);
        Assert.True(completed.IsCompleted);
        Assert.True(next.IsAccessible);
    }

    // Feature: elearning-app-zimbabwe, Property 14: Progress Percentage Calculation — Validates: Requirements 4.8
    [Property(MaxTest = 100)]
    public async Task Property14_displayed_percent_equals_completed_over_total(byte totalRaw, byte completedRaw)
    {
        int total = (totalRaw % 10) + 1;
        int completed = completedRaw % (total + 1);
        using ServiceProvider provider = ProgressPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        (EduZimDbContext db, IMediator mediator, _, MutableCurrentUser current) =
            ProgressPropertyFlow.Resolve(provider, scope);

        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        await ProgressPropertySeeds.SeedStudentAsync(db, tenantId, studentId).ConfigureAwait(false);
        List<Guid> moduleIds = await ProgressPropertySeeds
            .SeedModuleSequenceAsync(db, tenantId, "Science", total)
            .ConfigureAwait(false);
        for (int i = 0; i < completed; i++)
        {
            await ProgressPropertySeeds
                .SeedCompletedAsync(db, tenantId, studentId, moduleIds[i])
                .ConfigureAwait(false);
        }

        ProgressPropertyFlow.AsTeacher(current, tenantId);
        StudentProgressDto dto = await mediator
            .Send(new GetStudentProgressQuery(tenantId, studentId), CancellationToken.None)
            .ConfigureAwait(false);

        SubjectProgressDto subject = Assert.Single(dto.Subjects);
        Assert.Equal(ProgressPropertyFlow.OraclePercent(completed, total), subject.ProgressPercent);
    }

    // Feature: elearning-app-zimbabwe, Property 29: Parent Dashboard Data Completeness — Validates: Requirements 10.1
    [Property(MaxTest = 100)]
    public async Task Property29_dashboard_includes_required_fields_for_each_linked_student(byte countRaw)
    {
        int studentCount = (countRaw % 4) + 1;
        using ServiceProvider provider = ProgressPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        (EduZimDbContext db, IMediator mediator, _, MutableCurrentUser current) =
            ProgressPropertyFlow.Resolve(provider, scope);

        Guid tenantId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        await ProgressPropertySeeds.SeedParentAsync(db, tenantId, parentId).ConfigureAwait(false);
        await ProgressPropertySeeds.SeedModuleSequenceAsync(db, tenantId, "English", 2).ConfigureAwait(false);
        List<Guid> studentIds = await ProgressPropertyFlow
            .SeedLinkedStudentsAsync(db, tenantId, parentId, studentCount)
            .ConfigureAwait(false);

        ProgressPropertyFlow.AsParent(current, tenantId, parentId);
        ParentDashboardDto dto = await mediator
            .Send(new GetParentDashboardQuery(tenantId, parentId), CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(studentCount, dto.Students.Count);
        Assert.All(studentIds, id => Assert.Contains(dto.Students, s => s.StudentId == id));
        Assert.All(dto.Students, ProgressPropertyFlow.AssertDashboardComplete);
    }

    // Feature: elearning-app-zimbabwe, Property 31: Screen Time Limit Enforcement — Validates: Requirements 10.5
    [Property(MaxTest = 100)]
    public async Task Property31_limit_pauses_session_and_blocks_until_next_day(byte limitRaw, byte extraRaw)
    {
        int limit = (limitRaw % 500) + 30;
        int extraElapsed = (extraRaw % 40) + 1;
        using ServiceProvider provider = ProgressPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        (EduZimDbContext db, IMediator mediator, _, MutableCurrentUser current) =
            ProgressPropertyFlow.Resolve(provider, scope);

        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        await ProgressPropertySeeds.SeedStudentAsync(db, tenantId, studentId, limit).ConfigureAwait(false);
        DateOnly today = ScreenTimeLimitRules.CalendarDay(DateTime.UtcNow);
        await ProgressPropertySeeds
            .SeedSessionAsync(
                db,
                tenantId,
                studentId,
                SessionStatus.Active,
                today,
                accumulatedSeconds: Math.Max(0, limit - 1),
                lastHeartbeatAt: DateTime.UtcNow.AddSeconds(-(extraElapsed + 1)))
            .ConfigureAwait(false);

        ProgressPropertyFlow.AsStudent(current, tenantId, studentId);
        StudentSessionDto paused = await mediator
            .Send(new RecordStudentSessionHeartbeatCommand(tenantId, studentId), CancellationToken.None)
            .ConfigureAwait(false);
        Assert.Equal(SessionStatus.Paused, paused.Status);
        Assert.True(paused.LimitReached);

        await Assert.ThrowsAsync<ConflictException>(
            () => mediator.Send(new StartStudentSessionCommand(tenantId, studentId), CancellationToken.None));

        StudentSession row = await db.StudentSessions.SingleAsync().ConfigureAwait(false);
        row.SessionDate = today.AddDays(-1);
        await db.SaveChangesAsync().ConfigureAwait(false);

        StudentSessionDto nextDay = await mediator
            .Send(new StartStudentSessionCommand(tenantId, studentId), CancellationToken.None)
            .ConfigureAwait(false);
        Assert.Equal(SessionStatus.Active, nextDay.Status);
        Assert.False(nextDay.LimitReached);
    }
}
