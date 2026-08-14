using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;

namespace EduZim.Tests.Properties.LiveClassrooms;

internal static class LiveClassroomPropertySeeds
{
    public static async Task SeedTenantAndClassAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid classId,
        DateTime utcNow)
    {
        await db.Tenants.AddAsync(
                new Tenant
                {
                    Id = tenantId,
                    Name = "School",
                    Tier = TenantTier.School,
                    Status = TenantStatus.Active,
                    Branding = new BrandingSettings { SchoolName = "School", PrimaryColour = "#1976D2" },
                    CreatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SchoolClasses.AddAsync(
                new SchoolClass
                {
                    Id = classId,
                    TenantId = tenantId,
                    Name = "4A",
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
    }

    public static async Task SeedUserAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid userId,
        UserRole role)
    {
        string userName = userId.ToString("N");
        await db.Users.AddAsync(
                new ApplicationUser
                {
                    Id = userId,
                    TenantId = tenantId,
                    Role = role,
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    EmailConfirmed = true,
                })
            .ConfigureAwait(false);
    }

    public static async Task<(List<Guid> StudentIds, List<Guid> ParentIds)> SeedClassRosterAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid classId,
        int studentCount,
        bool withParents,
        DateTime utcNow,
        bool seedUsers = true)
    {
        List<Guid> studentIds = [];
        List<Guid> parentIds = [];
        for (int i = 0; i < studentCount; i++)
        {
            Guid studentId = Guid.NewGuid();
            studentIds.Add(studentId);
            if (seedUsers)
                await SeedUserAsync(db, tenantId, studentId, UserRole.Student).ConfigureAwait(false);
            await db.ClassEnrollments.AddAsync(
                    new ClassEnrollment
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        SchoolClassId = classId,
                        StudentUserId = studentId,
                        CreatedAt = utcNow,
                        UpdatedAt = utcNow,
                    })
                .ConfigureAwait(false);
            if (!withParents)
                continue;

            Guid parentId = Guid.NewGuid();
            parentIds.Add(parentId);
            if (seedUsers)
                await SeedUserAsync(db, tenantId, parentId, UserRole.ParentGuardian).ConfigureAwait(false);
            await db.ParentStudentLinks.AddAsync(
                    new ParentStudentLink
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ParentUserId = parentId,
                        StudentUserId = studentId,
                        CreatedAt = utcNow,
                        UpdatedAt = utcNow,
                    })
                .ConfigureAwait(false);
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
        return (studentIds, parentIds);
    }

    public static async Task SeedOpenSessionAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid classId,
        Guid sessionId,
        Guid teacherId,
        DateTime startAt)
    {
        DateTime utcNow = DateTime.UtcNow;
        await db.ClassroomSessions.AddAsync(
                new ClassroomSession
                {
                    Id = sessionId,
                    TenantId = tenantId,
                    SchoolClassId = classId,
                    TeacherUserId = teacherId,
                    StartAtUtc = startAt,
                    PlannedEndAtUtc = startAt.AddMinutes(45),
                    RoomId = sessionId.ToString("N"),
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedParticipantAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid sessionId,
        Guid userId,
        DateTime joinedAt)
    {
        await db.ClassroomParticipants.AddAsync(
                new ClassroomParticipant
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ClassroomSessionId = sessionId,
                    UserId = userId,
                    JoinedAtUtc = joinedAt,
                    CreatedAt = joinedAt,
                    UpdatedAt = joinedAt,
                })
            .ConfigureAwait(false);
    }

    public static async Task SeedEndedSessionAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid classId,
        Guid sessionId,
        Guid teacherId,
        DateTime endedAt,
        string recordingUrl)
    {
        DateTime startAt = endedAt.AddMinutes(-45);
        await db.ClassroomSessions.AddAsync(
                new ClassroomSession
                {
                    Id = sessionId,
                    TenantId = tenantId,
                    SchoolClassId = classId,
                    TeacherUserId = teacherId,
                    StartAtUtc = startAt,
                    PlannedEndAtUtc = endedAt,
                    SessionEndTime = endedAt,
                    RoomId = sessionId.ToString("N"),
                    RecordingUrl = recordingUrl,
                    CreatedAt = startAt,
                    UpdatedAt = endedAt,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task<Dictionary<Guid, DateTime>> SeedJoinedStudentsAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid sessionId,
        Guid teacherId,
        List<Guid> studentIds,
        int joinedCount)
    {
        DateTime utcNow = DateTime.UtcNow;
        await SeedParticipantAsync(db, tenantId, sessionId, teacherId, utcNow.AddMinutes(-20))
            .ConfigureAwait(false);
        Dictionary<Guid, DateTime> joinTimes = [];
        for (int i = 0; i < joinedCount; i++)
        {
            DateTime joinedAt = utcNow.AddMinutes(-(5 + (i * 3)));
            joinTimes[studentIds[i]] = joinedAt;
            await SeedParticipantAsync(db, tenantId, sessionId, studentIds[i], joinedAt)
                .ConfigureAwait(false);
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
        return joinTimes;
    }
}
