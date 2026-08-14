using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;

namespace EduZim.Tests.Properties.Marketplace;

internal sealed record MarketplaceSchools(
    Guid OriginTenantId,
    Guid OriginTeacherId,
    Guid RequestingTenantId,
    Guid RequestingTeacherId,
    Guid OriginContentItemId);

internal static class MarketplacePropertySeeds
{
    public static async Task<MarketplaceSchools> SeedTwoSchoolsAsync(
        EduZimDbContext db,
        string originSchoolName,
        string originTeacherName)
    {
        Guid originTenantId = Guid.NewGuid();
        Guid originTeacherId = Guid.NewGuid();
        Guid requestingTenantId = Guid.NewGuid();
        Guid requestingTeacherId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        DateTime utcNow = DateTime.UtcNow;

        await SeedSchoolAsync(db, originTenantId, originSchoolName, utcNow).ConfigureAwait(false);
        await SeedTeacherAsync(db, originTenantId, originTeacherId, originTeacherName).ConfigureAwait(false);
        await SeedContentItemAsync(db, originTenantId, originTeacherId, itemId, utcNow).ConfigureAwait(false);
        await SeedSchoolAsync(db, requestingTenantId, "Requesting School", utcNow).ConfigureAwait(false);
        await SeedTeacherAsync(db, requestingTenantId, requestingTeacherId, "Nomsa Dube").ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);

        return new MarketplaceSchools(
            originTenantId,
            originTeacherId,
            requestingTenantId,
            requestingTeacherId,
            itemId);
    }

    public static async Task<Guid> SeedApprovedPackAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid teacherId,
        Guid contentItemId,
        string title,
        string schoolName,
        string teacherName)
    {
        DateTime utcNow = DateTime.UtcNow;
        Guid packId = Guid.NewGuid();
        await db.ContentPacks.AddAsync(
                new ContentPack
                {
                    Id = packId,
                    TenantId = tenantId,
                    Title = title,
                    Description = "Seeded pack",
                    Status = ContentPackStatus.Approved,
                    SubmittedByUserId = teacherId,
                    SchoolName = schoolName,
                    TeacherName = teacherName,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.ContentPackItems.AddAsync(
                new ContentPackItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ContentPackId = packId,
                    ContentItemId = contentItemId,
                    SequenceOrder = 0,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return packId;
    }

    public static async Task<Guid> SeedContentItemAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid teacherId,
        DateTime utcNow)
    {
        Guid itemId = Guid.NewGuid();
        await SeedContentItemAsync(db, tenantId, teacherId, itemId, utcNow).ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);
        return itemId;
    }

    private static async Task SeedSchoolAsync(
        EduZimDbContext db,
        Guid tenantId,
        string schoolName,
        DateTime utcNow)
    {
        await db.Tenants.AddAsync(
                new Tenant
                {
                    Id = tenantId,
                    Name = schoolName,
                    Tier = TenantTier.School,
                    Status = TenantStatus.Active,
                    Branding = new BrandingSettings { SchoolName = schoolName, PrimaryColour = "#1976D2" },
                    CreatedAt = utcNow,
                })
            .ConfigureAwait(false);
    }

    private static async Task SeedTeacherAsync(EduZimDbContext db, Guid tenantId, Guid userId, string fullName)
    {
        string userName = userId.ToString("N");
        await db.Users.AddAsync(
                new ApplicationUser
                {
                    Id = userId,
                    TenantId = tenantId,
                    Role = UserRole.Teacher,
                    FullName = fullName,
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    EmailConfirmed = true,
                })
            .ConfigureAwait(false);
    }

    private static async Task SeedContentItemAsync(
        EduZimDbContext db,
        Guid tenantId,
        Guid teacherId,
        Guid itemId,
        DateTime utcNow)
    {
        await db.ContentItems.AddAsync(
                new ContentItem
                {
                    Id = itemId,
                    TenantId = tenantId,
                    Title = "Fractions",
                    Type = ContentType.Video,
                    StorageKey = $"{tenantId}/content/{itemId}/file",
                    FileSizeBytes = 2048,
                    Language = "en",
                    Status = ContentStatus.Published,
                    UploadedByUserId = teacherId,
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow,
                })
            .ConfigureAwait(false);
    }
}
