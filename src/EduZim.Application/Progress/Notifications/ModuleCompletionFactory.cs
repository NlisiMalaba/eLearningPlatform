using EduZim.Domain.Entities;
using EduZim.Domain.Events;

namespace EduZim.Application.Progress.Notifications;

internal static class ModuleCompletionFactory
{
    public static StudentProgress Progress(
        ModuleCompletedNotification notification,
        Guid moduleId,
        DateTime utcNow,
        bool completed,
        bool unlocked)
    {
        return new StudentProgress
        {
            Id = Guid.NewGuid(),
            TenantId = notification.TenantId,
            StudentId = notification.StudentId,
            ModuleId = moduleId,
            IsCompleted = completed,
            IsUnlocked = unlocked,
            CompletedAt = completed ? utcNow : null,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static SubjectProgress Subject(
        ModuleCompletedNotification notification,
        string subject,
        int percent,
        DateTime utcNow)
    {
        return new SubjectProgress
        {
            Id = Guid.NewGuid(),
            TenantId = notification.TenantId,
            StudentId = notification.StudentId,
            Subject = subject,
            ProgressPercent = percent,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }
}
