using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

internal static class LiveClassroomTestData
{
    public static ClassroomSession Session(
        Guid tenantId,
        Guid sessionId,
        Guid classId,
        Guid teacherId,
        DateTime startAt,
        DateTime? endedAt = null,
        string? recordingUrl = null)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ClassroomSession
        {
            Id = sessionId,
            TenantId = tenantId,
            SchoolClassId = classId,
            TeacherUserId = teacherId,
            StartAtUtc = startAt,
            PlannedEndAtUtc = startAt.AddMinutes(45),
            SessionEndTime = endedAt,
            RoomId = sessionId.ToString("N"),
            RecordingUrl = recordingUrl,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static SchoolClass Class(Guid tenantId, Guid classId, string name = "Grade 4A")
    {
        DateTime utcNow = DateTime.UtcNow;
        return new SchoolClass
        {
            Id = classId,
            TenantId = tenantId,
            Name = name,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ClassEnrollment Enrollment(Guid tenantId, Guid classId, Guid studentId)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new ClassEnrollment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SchoolClassId = classId,
            StudentUserId = studentId,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ClassroomParticipant Participant(Guid tenantId, Guid sessionId, Guid userId, DateTime joinedAt)
    {
        return new ClassroomParticipant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassroomSessionId = sessionId,
            UserId = userId,
            JoinedAtUtc = joinedAt,
            CreatedAt = joinedAt,
            UpdatedAt = joinedAt,
        };
    }

    public static Mock<ICurrentUser> CurrentUser(Guid tenantId, UserRole role, Guid userId)
    {
        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(userId);
        return user;
    }
}
