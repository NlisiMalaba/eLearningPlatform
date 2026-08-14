using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;

namespace EduZim.Tests.Properties.Progress;

internal static class ProgressPropertySeeds
{
    public static async Task SeedStudentAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        int? dailyLimitSeconds = null)
    {
        string userName = studentId.ToString("N");
        await db.Users.AddAsync(
                new ApplicationUser
                {
                    Id = studentId,
                    TenantId = tenantId,
                    Role = UserRole.Student,
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    EmailConfirmed = true,
                    DailyScreenTimeLimitSeconds = dailyLimitSeconds,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedParentAsync(EduZimDbContext db, Guid tenantId, Guid parentId)
    {
        string userName = parentId.ToString("N");
        await db.Users.AddAsync(
                new ApplicationUser
                {
                    Id = parentId,
                    TenantId = tenantId,
                    Role = UserRole.ParentGuardian,
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    EmailConfirmed = true,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedLinkAsync(EduZimDbContext db, Guid tenantId, Guid parentId, Guid studentId)
    {
        DateTime utcNow = DateTime.UtcNow;
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
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task<List<Guid>> SeedModuleSequenceAsync(
        EduZimDbContext db,
        Guid tenantId,
        string subject,
        int count)
    {
        DateTime utcNow = DateTime.UtcNow;
        List<Guid> ids = new();
        for (int i = 0; i < count; i++)
        {
            Guid id = Guid.NewGuid();
            ids.Add(id);
            await db.Modules.AddAsync(
                    new Module
                    {
                        Id = id,
                        TenantId = tenantId,
                        Title = $"{subject} {i + 1}",
                        Subject = subject,
                        Grade = GradeLevel.Grade1,
                        SequenceOrder = i + 1,
                        CreatedAt = utcNow,
                        UpdatedAt = utcNow,
                    })
                .ConfigureAwait(false);
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
        return ids;
    }

    public static async Task SeedCompletedAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid moduleId)
    {
        DateTime utcNow = DateTime.UtcNow;
        await db.StudentProgresses.AddAsync(
                new StudentProgress
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = studentId,
                    ModuleId = moduleId,
                    IsCompleted = true,
                    IsUnlocked = true,
                    CompletedAt = utcNow,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task SeedSessionAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        SessionStatus status,
        DateOnly sessionDate,
        int accumulatedSeconds,
        DateTime lastHeartbeatAt)
    {
        await db.StudentSessions.AddAsync(
                new StudentSession
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = studentId,
                    Status = status,
                    SessionDate = sessionDate,
                    StartedAt = lastHeartbeatAt,
                    LastHeartbeatAt = lastHeartbeatAt,
                    AccumulatedSeconds = accumulatedSeconds,
                    CreatedAt = lastHeartbeatAt,
                    UpdatedAt = lastHeartbeatAt,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
