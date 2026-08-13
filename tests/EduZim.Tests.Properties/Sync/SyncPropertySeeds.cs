using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;

namespace EduZim.Tests.Properties.Sync;

internal static class SyncPropertySeeds
{
    public static async Task SeedConflictScenarioAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        DateTime serverTimestamp,
        int serverTimeOnTaskSeconds,
        bool serverCompleted)
    {
        await db.Tenants.AddAsync(
                new Tenant
                {
                    Id = tenantId,
                    Name = "School",
                    Tier = TenantTier.School,
                    Status = TenantStatus.Active,
                    Branding = new BrandingSettings { SchoolName = "School", PrimaryColour = "#1976D2" },
                    CreatedAt = serverTimestamp,
                })
            .ConfigureAwait(false);

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
                })
            .ConfigureAwait(false);

        await db.Modules.AddAsync(
                new Module
                {
                    Id = moduleId,
                    TenantId = tenantId,
                    Title = "Numbers",
                    Subject = "Maths",
                    Grade = GradeLevel.Grade1,
                    SequenceOrder = 1,
                    CreatedAt = serverTimestamp,
                    UpdatedAt = serverTimestamp,
                })
            .ConfigureAwait(false);

        await db.StudentProgresses.AddAsync(
                new StudentProgress
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = studentId,
                    ModuleId = moduleId,
                    IsCompleted = serverCompleted,
                    CompletedAt = serverCompleted ? serverTimestamp : null,
                    TimeOnTaskSeconds = serverTimeOnTaskSeconds,
                    CreatedAt = serverTimestamp,
                    UpdatedAt = serverTimestamp,
                })
            .ConfigureAwait(false);

        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
