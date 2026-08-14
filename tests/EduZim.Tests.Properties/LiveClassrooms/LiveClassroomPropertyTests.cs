using EduZim.Application.LiveClassrooms.Commands.EndSession;
using EduZim.Application.LiveClassrooms.Commands.ScheduleSession;
using EduZim.Application.LiveClassrooms.Queries.GetRecordingUrl;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck.Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.LiveClassrooms;

/// <summary>Feature: elearning-app-zimbabwe — Live Classroom properties 34, 35, 36.</summary>
[Collection(nameof(LiveClassroomPropertyTests))]
public sealed class LiveClassroomPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 34: Live Classroom Notification Lead Time — Validates: Requirements 13.2
    [Property(MaxTest = 100)]
    public async Task Property34_reminders_exist_for_students_and_parents_at_least_24h_before_start(
        byte studentCountRaw,
        byte extraHoursRaw,
        bool withParents)
    {
        int studentCount = (studentCountRaw % 5) + 1;
        int extraHours = (extraHoursRaw % 48) + 1;
        using ServiceProvider provider = LiveClassroomPropertyTestHost.Create();
        using IServiceScope scope = LiveClassroomPropertyTestHost.CreateScope(provider);
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        MutableCurrentUser current = provider.GetRequiredService<MutableCurrentUser>();

        DateTime utcNow = DateTime.UtcNow;
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        DateTime startAt = utcNow.Add(ClassroomReminderRules.LeadTime).AddHours(extraHours);
        await LiveClassroomPropertySeeds.SeedTenantAndClassAsync(db, tenantId, classId, utcNow)
            .ConfigureAwait(false);
        await LiveClassroomPropertySeeds.SeedUserAsync(db, tenantId, teacherId, UserRole.Teacher)
            .ConfigureAwait(false);
        (List<Guid> studentIds, List<Guid> parentIds) = await LiveClassroomPropertySeeds
            .SeedClassRosterAsync(db, tenantId, classId, studentCount, withParents, utcNow)
            .ConfigureAwait(false);

        current.UserId = teacherId;
        current.TenantId = tenantId;
        current.Role = UserRole.Teacher;
        Guid sessionId = await mediator
            .Send(new ScheduleSessionCommand(tenantId, classId, startAt, 45), CancellationToken.None)
            .ConfigureAwait(false);

        await AssertLeadTimeAsync(db, tenantId, sessionId, startAt, studentIds, parentIds).ConfigureAwait(false);
    }

    // Feature: elearning-app-zimbabwe, Property 35: Attendance Record on Session End — Validates: Requirements 13.5
    [Property(MaxTest = 100)]
    public async Task Property35_ending_session_writes_join_time_and_duration_for_each_joined_student(
        byte studentCountRaw,
        byte joinedCountRaw)
    {
        int studentCount = (studentCountRaw % 6) + 1;
        int joinedCount = joinedCountRaw % (studentCount + 1);
        using ServiceProvider provider = LiveClassroomPropertyTestHost.Create();
        using IServiceScope scope = LiveClassroomPropertyTestHost.CreateScope(provider);
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        MutableCurrentUser current = provider.GetRequiredService<MutableCurrentUser>();

        DateTime utcNow = DateTime.UtcNow;
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        await LiveClassroomPropertySeeds.SeedTenantAndClassAsync(db, tenantId, classId, utcNow)
            .ConfigureAwait(false);
        (List<Guid> studentIds, List<Guid> _) = await LiveClassroomPropertySeeds
            .SeedClassRosterAsync(db, tenantId, classId, studentCount, withParents: false, utcNow, seedUsers: false)
            .ConfigureAwait(false);
        await LiveClassroomPropertySeeds
            .SeedOpenSessionAsync(db, tenantId, classId, sessionId, teacherId, utcNow.AddHours(-1))
            .ConfigureAwait(false);
        Dictionary<Guid, DateTime> joinTimes = await LiveClassroomPropertySeeds.SeedJoinedStudentsAsync(
                db,
                tenantId,
                sessionId,
                teacherId,
                studentIds,
                joinedCount)
            .ConfigureAwait(false);

        current.UserId = teacherId;
        current.TenantId = tenantId;
        current.Role = UserRole.Teacher;
        await mediator.Send(new EndSessionCommand(tenantId, sessionId), CancellationToken.None)
            .ConfigureAwait(false);

        await AssertAttendanceAsync(db, tenantId, sessionId, joinTimes).ConfigureAwait(false);
    }

    // Feature: elearning-app-zimbabwe, Property 36: Recording Availability Window — Validates: Requirements 13.7
    [Property(MaxTest = 100)]
    public async Task Property36_recording_url_is_available_for_exactly_30_days_after_end(byte daysAgoRaw)
    {
        int daysAgo = daysAgoRaw % 46;
        using ServiceProvider provider = LiveClassroomPropertyTestHost.Create();
        using IServiceScope scope = LiveClassroomPropertyTestHost.CreateScope(provider);
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        MutableCurrentUser current = provider.GetRequiredService<MutableCurrentUser>();

        DateTime utcNow = DateTime.UtcNow;
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        DateTime endedAt = utcNow.AddDays(-daysAgo).AddMinutes(-5);
        const string recordingUrl = "https://recordings.test/session";
        await LiveClassroomPropertySeeds.SeedTenantAndClassAsync(db, tenantId, classId, utcNow)
            .ConfigureAwait(false);
        await LiveClassroomPropertySeeds
            .SeedEndedSessionAsync(db, tenantId, classId, sessionId, teacherId, endedAt, recordingUrl)
            .ConfigureAwait(false);

        current.UserId = teacherId;
        current.TenantId = tenantId;
        current.Role = UserRole.Teacher;
        string? url = await mediator
            .Send(new GetRecordingUrlQuery(tenantId, sessionId), CancellationToken.None)
            .ConfigureAwait(false);

        bool expectedAccessible = RecordingAvailabilityRules.IsAccessible(endedAt, DateTime.UtcNow);
        if (expectedAccessible)
            Assert.Equal(recordingUrl, url);
        else
            Assert.Null(url);
    }

    private static async Task AssertLeadTimeAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid sessionId,
        DateTime startAt,
        List<Guid> studentIds,
        List<Guid> parentIds)
    {
        List<Notification> rows = await db.Notifications.AsNoTracking()
            .Where(n => n.TenantId == tenantId && n.Type == NotificationType.LiveClassroomReminder)
            .ToListAsync()
            .ConfigureAwait(false);
        HashSet<Guid> expected = studentIds.Concat(parentIds).ToHashSet();
        HashSet<Guid> actual = rows.Select(n => n.UserId).ToHashSet();
        Assert.True(expected.SetEquals(actual));
        DateTime notifyBy = ClassroomReminderRules.NotifyAtOrBeforeUtc(startAt);
        Assert.All(rows, n => Assert.True(n.CreatedAt <= notifyBy));
        ClassroomSession session = await db.ClassroomSessions.AsNoTracking()
            .SingleAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);
        Assert.Equal(startAt, session.StartAtUtc);
    }

    private static async Task AssertAttendanceAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid sessionId,
        Dictionary<Guid, DateTime> joinTimes)
    {
        ClassroomSession session = await db.ClassroomSessions.AsNoTracking()
            .SingleAsync(s => s.Id == sessionId)
            .ConfigureAwait(false);
        Assert.NotNull(session.SessionEndTime);
        List<AttendanceRecord> rows = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.ClassroomSessionId == sessionId)
            .ToListAsync()
            .ConfigureAwait(false);
        Assert.Equal(joinTimes.Count, rows.Count);
        Assert.True(joinTimes.Keys.ToHashSet().SetEquals(rows.Select(r => r.StudentUserId)));
        foreach (AttendanceRecord row in rows)
        {
            DateTime join = joinTimes[row.StudentUserId];
            int expectedDuration = (int)Math.Max(0, (session.SessionEndTime.Value - join).TotalSeconds);
            Assert.Equal(join, row.JoinTimeUtc);
            Assert.Equal(expectedDuration, row.DurationSeconds);
        }
    }
}
