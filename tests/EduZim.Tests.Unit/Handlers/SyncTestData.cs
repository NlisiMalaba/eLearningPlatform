using EduZim.Application.Common.Interfaces;
using EduZim.Application.Sync.DTOs;
using EduZim.Application.Sync.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MockQueryable.Moq;
using Moq;
using System.Text.Json;

namespace EduZim.Tests.Unit.Handlers;

internal static class SyncTestData
{
    public static ApplicationUser Student(Guid tenantId, Guid studentId)
    {
        return new ApplicationUser
        {
            Id = studentId,
            TenantId = tenantId,
            Role = UserRole.Student,
        };
    }

    public static Module ModuleRow(Guid tenantId, Guid moduleId)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new Module
        {
            Id = moduleId,
            TenantId = tenantId,
            Title = "Numbers",
            Subject = "Maths",
            SequenceOrder = 1,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static Assessment AssessmentRow(Guid tenantId, Guid assessmentId, Guid moduleId)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new Assessment
        {
            Id = assessmentId,
            TenantId = tenantId,
            ModuleId = moduleId,
            Title = "Quiz",
            PassingScorePercent = 60,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static StudentProgress Progress(
        Guid tenantId,
        Guid studentId,
        Guid moduleId,
        DateTime timestamp,
        bool isCompleted = false,
        int timeOnTaskSeconds = 10)
    {
        return new StudentProgress
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            ModuleId = moduleId,
            IsCompleted = isCompleted,
            CompletedAt = isCompleted ? timestamp : null,
            TimeOnTaskSeconds = timeOnTaskSeconds,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
        };
    }

    public static AssessmentAttempt Attempt(
        Guid tenantId,
        Guid studentId,
        Guid assessmentId,
        DateTime timestamp,
        int scorePercent = 70)
    {
        return new AssessmentAttempt
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AssessmentId = assessmentId,
            StudentId = studentId,
            StartedAt = timestamp.AddMinutes(-10),
            ScorePercent = scorePercent,
            TimeTakenSeconds = 600,
            SubmittedAt = timestamp,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
        };
    }

    public static OfflineSyncQueue QueueItem(
        Guid tenantId,
        Guid studentId,
        DateTime localTimestamp,
        OfflineSyncPayloadDto payload,
        SyncStatus status = SyncStatus.Pending,
        Guid? id = null)
    {
        return new OfflineSyncQueue
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            Payload = JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            LocalTimestamp = localTimestamp,
            Status = status,
        };
    }

    public static OfflineSyncPayloadDto ModulePayload(
        Guid moduleId,
        bool isCompleted = true,
        int timeOnTaskSeconds = 120,
        DateTime? completedAt = null)
    {
        return new OfflineSyncPayloadDto(
            OfflineSyncKinds.ModuleProgress,
            ModuleId: moduleId,
            IsCompleted: isCompleted,
            CompletedAt: completedAt,
            TimeOnTaskSeconds: timeOnTaskSeconds);
    }

    public static OfflineSyncPayloadDto AttemptPayload(
        Guid assessmentId,
        int scorePercent,
        int timeTakenSeconds,
        DateTime submittedAt,
        Guid? attemptId = null)
    {
        return new OfflineSyncPayloadDto(
            OfflineSyncKinds.AssessmentAttempt,
            AssessmentId: assessmentId,
            AttemptId: attemptId,
            ScorePercent: scorePercent,
            TimeTakenSeconds: timeTakenSeconds,
            SubmittedAt: submittedAt);
    }

    public static Mock<ICurrentUser> CurrentUser(Guid tenantId, UserRole role, Guid userId)
    {
        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);
        return user;
    }

    public static Mock<DbSet<T>> MockSet<T>(List<T> rows) where T : class
    {
        Mock<DbSet<T>> set = rows.AsQueryable().BuildMockDbSet();
        set.Setup(s => s.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => rows.Add(entity))
            .Returns(new ValueTask<EntityEntry<T>>((EntityEntry<T>)null!));
        return set;
    }
}
