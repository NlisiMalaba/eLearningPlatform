using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;

namespace EduZim.Tests.Properties.Notifications;

internal static class NotificationPropertySeeds
{
    public static async Task SeedActiveTenantAsync(EduZimDbContext db, Guid tenantId, DateTime utcNow)
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
    }

    public static async Task SeedUserAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid userId,
        UserRole role,
        DateTime? lastLoginAt = null,
        string? email = null,
        string? phone = null)
    {
        string address = email ?? $"{userId:N}@test.local";
        await db.Users.AddAsync(
                new ApplicationUser
                {
                    Id = userId,
                    TenantId = tenantId,
                    Role = role,
                    UserName = address,
                    NormalizedUserName = address.ToUpperInvariant(),
                    Email = address,
                    NormalizedEmail = address.ToUpperInvariant(),
                    PhoneNumber = phone,
                    LastLoginAt = lastLoginAt,
                    EmailConfirmed = true,
                })
            .ConfigureAwait(false);
    }

    public static async Task<List<Guid>> SeedInactiveFamilyAsync(
        EduZimDbContext db,
        Guid tenantId,
        int parentCount,
        int daysInactive)
    {
        DateTime utcNow = DateTime.UtcNow;
        Guid studentId = Guid.NewGuid();
        await SeedActiveTenantAsync(db, tenantId, utcNow).ConfigureAwait(false);
        await SeedUserAsync(
                db,
                tenantId,
                studentId,
                UserRole.Student,
                lastLoginAt: utcNow.AddDays(-daysInactive))
            .ConfigureAwait(false);

        List<Guid> parentIds = [];
        for (int i = 0; i < parentCount; i++)
        {
            Guid parentId = Guid.NewGuid();
            parentIds.Add(parentId);
            await SeedUserAsync(
                    db,
                    tenantId,
                    parentId,
                    UserRole.ParentGuardian,
                    email: $"{parentId:N}@parent.local",
                    phone: "+263771000000")
                .ConfigureAwait(false);
            await SeedParentLinkAsync(db, tenantId, parentId, studentId, utcNow).ConfigureAwait(false);
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
        return parentIds;
    }

    public static async Task SeedNotifiableUserAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid userId,
        NotificationType type,
        bool inApp,
        bool email,
        bool sms)
    {
        DateTime utcNow = DateTime.UtcNow;
        await SeedActiveTenantAsync(db, tenantId, utcNow).ConfigureAwait(false);
        await SeedUserAsync(
                db,
                tenantId,
                userId,
                UserRole.Student,
                email: $"{userId:N}@student.local",
                phone: "+263771000000")
            .ConfigureAwait(false);
        await SeedPreferenceAsync(db, tenantId, userId, type, inApp, email, sms, utcNow).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    public static async Task<Guid> SeedFailedSmsScenarioAsync(EduZimDbContext db, Guid tenantId, Guid userId)
    {
        DateTime utcNow = DateTime.UtcNow;
        await SeedActiveTenantAsync(db, tenantId, utcNow).ConfigureAwait(false);
        await SeedUserAsync(db, tenantId, userId, UserRole.Student, phone: "+263771000000")
            .ConfigureAwait(false);
        Guid notificationId = await SeedFailedSmsAsync(db, tenantId, userId, utcNow).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return notificationId;
    }

    public static async Task SeedParentLinkAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid parentId,
        Guid studentId,
        DateTime utcNow)
    {
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

    public static async Task SeedPreferenceAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid userId,
        NotificationType type,
        bool inApp,
        bool email,
        bool sms,
        DateTime utcNow)
    {
        await db.NotificationPreferences.AddAsync(
                new NotificationPreference
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = userId,
                    Type = type,
                    InAppEnabled = inApp,
                    EmailEnabled = email,
                    SmsEnabled = sms,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
    }

    public static async Task<Guid> SeedFailedSmsAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid userId,
        DateTime utcNow)
    {
        Guid id = Guid.NewGuid();
        await db.Notifications.AddAsync(
                new Notification
                {
                    Id = id,
                    TenantId = tenantId,
                    UserId = userId,
                    Type = NotificationType.SubscriptionExpiry,
                    Message = "Expires soon",
                    Channel = NotificationChannel.Sms,
                    Status = NotificationStatus.Failed,
                    RetryCount = 0,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        return id;
    }
}
