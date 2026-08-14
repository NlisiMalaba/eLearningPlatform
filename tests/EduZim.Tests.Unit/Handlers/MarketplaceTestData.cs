using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

internal static class MarketplaceTestData
{
    public static Mock<ICurrentUser> CurrentUser(Guid? tenantId, UserRole role, Guid userId)
    {
        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);
        return user;
    }

    public static Tenant School(Guid tenantId, string name, string schoolName)
    {
        return new Tenant
        {
            Id = tenantId,
            Name = name,
            Tier = TenantTier.School,
            Status = TenantStatus.Active,
            Branding = new BrandingSettings { SchoolName = schoolName, PrimaryColour = "#1976D2" },
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static ApplicationUser Teacher(Guid tenantId, Guid userId, string fullName)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            Role = UserRole.Teacher,
            FullName = fullName,
        };
    }

    public static ContentItem Item(Guid tenantId, Guid itemId, string title = "Fractions video")
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ContentItem
        {
            Id = itemId,
            TenantId = tenantId,
            Title = title,
            Type = ContentType.Video,
            StorageKey = $"{tenantId}/content/{itemId}/file",
            FileSizeBytes = 1024,
            Language = "en",
            Status = ContentStatus.Published,
            UploadedByUserId = Guid.NewGuid(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ContentPack Pack(
        Guid tenantId,
        Guid packId,
        Guid teacherId,
        ContentPackStatus status,
        string schoolName = "Mufakose Primary",
        string teacherName = "Agnes Moyo",
        string title = "Grade 4 Fractions")
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ContentPack
        {
            Id = packId,
            TenantId = tenantId,
            Title = title,
            Description = "Shared pack",
            Status = status,
            SubmittedByUserId = teacherId,
            SchoolName = schoolName,
            TeacherName = teacherName,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ContentPackItem PackItem(Guid tenantId, Guid packId, Guid contentItemId, int order = 0)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ContentPackItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ContentPackId = packId,
            ContentItemId = contentItemId,
            SequenceOrder = order,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ContentPackAccessRequest AccessRequest(
        Guid originatingTenantId,
        Guid packId,
        Guid requestingTenantId,
        Guid requestedByUserId,
        ContentPackAccessStatus status)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ContentPackAccessRequest
        {
            Id = Guid.NewGuid(),
            TenantId = originatingTenantId,
            ContentPackId = packId,
            RequestingTenantId = requestingTenantId,
            RequestedByUserId = requestedByUserId,
            Status = status,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ContentPackRating Rating(Guid raterTenantId, Guid packId, Guid userId, int value)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ContentPackRating
        {
            Id = Guid.NewGuid(),
            TenantId = raterTenantId,
            ContentPackId = packId,
            UserId = userId,
            Rating = value,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static Mock<IEduZimDbContext> CreateDb(
        List<Tenant>? tenants = null,
        List<ApplicationUser>? users = null,
        List<ContentItem>? items = null,
        List<ContentPack>? packs = null,
        List<ContentPackItem>? packItems = null,
        List<ContentPackAccessRequest>? requests = null,
        List<ContentPackRating>? ratings = null)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Tenants).Returns((tenants ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns((users ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ContentItems).Returns((items ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ContentPacks).Returns(MockSet(packs ?? []).Object);
        db.Setup(x => x.ContentPackItems).Returns(MockSet(packItems ?? []).Object);
        db.Setup(x => x.ContentPackAccessRequests).Returns(MockSet(requests ?? []).Object);
        db.Setup(x => x.ContentPackRatings).Returns(MockSet(ratings ?? []).Object);
        return db;
    }

    private static Mock<DbSet<T>> MockSet<T>(List<T> rows)
        where T : class
    {
        Mock<DbSet<T>> set = rows.AsQueryable().BuildMockDbSet();
        set.Setup(s => s.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => rows.Add(entity))
            .Returns(new ValueTask<EntityEntry<T>>((EntityEntry<T>)null!));
        return set;
    }
}
