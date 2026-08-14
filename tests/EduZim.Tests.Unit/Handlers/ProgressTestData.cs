using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Handlers;

internal static class ProgressTestData
{
    public static Module Module(
        Guid tenantId,
        Guid id,
        string subject,
        int sequenceOrder,
        GradeLevel grade = GradeLevel.Grade1,
        string? title = null)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new Module
        {
            Id = id,
            TenantId = tenantId,
            Title = title ?? $"{subject} {sequenceOrder}",
            Subject = subject,
            Grade = grade,
            SequenceOrder = sequenceOrder,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static StudentProgress Progress(
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        bool completed,
        bool unlocked,
        DateTime? completedAt = null)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new StudentProgress
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            ModuleId = moduleId,
            IsCompleted = completed,
            IsUnlocked = unlocked,
            CompletedAt = completedAt,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ParentStudentLink Link(Guid tenantId, Guid parentId, Guid studentId)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ParentStudentLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ParentUserId = parentId,
            StudentUserId = studentId,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static Badge Badge(Guid tenantId, Guid studentId, BadgeType type, DateTime earnedAt)
    {
        return new Badge
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            Type = type,
            EarnedAt = earnedAt,
            CreatedAt = earnedAt,
            UpdatedAt = earnedAt,
        };
    }

    public static Tenant ActiveTenant(Guid tenantId)
    {
        return new Tenant
        {
            Id = tenantId,
            Name = "School",
            Tier = TenantTier.School,
            Status = TenantStatus.Active,
            Branding = new BrandingSettings { SchoolName = "School", PrimaryColour = "#1976D2" },
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static ApplicationUser Student(Guid tenantId, Guid studentId, int? dailyLimitSeconds = null)
    {
        return new ApplicationUser
        {
            Id = studentId,
            TenantId = tenantId,
            Role = UserRole.Student,
            DailyScreenTimeLimitSeconds = dailyLimitSeconds,
        };
    }

    public static StudentSession Session(
        Guid tenantId,
        Guid studentId,
        SessionStatus status,
        DateOnly sessionDate,
        int accumulatedSeconds,
        DateTime lastHeartbeatAt)
    {
        return new StudentSession
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
        };
    }
}
