using EduZim.Domain.Enums;

namespace EduZim.Application.Progress.DTOs;

public sealed record StudentProgressDto(
    Guid StudentId,
    IReadOnlyList<SubjectProgressDto> Subjects);

public sealed record SubjectProgressDto(
    string Subject,
    int ProgressPercent,
    IReadOnlyList<ModuleProgressDto> Modules);

public sealed record ModuleProgressDto(
    Guid ModuleId,
    string Title,
    GradeLevel Grade,
    int SequenceOrder,
    bool IsCompleted,
    bool IsAccessible,
    DateTime? CompletedAt);

public sealed record ParentDashboardDto(
    Guid ParentId,
    IReadOnlyList<LinkedStudentDashboardDto> Students);

public sealed record LinkedStudentDashboardDto(
    Guid StudentId,
    GradeLevel CurrentGrade,
    IReadOnlyList<string> Subjects,
    IReadOnlyList<RecentActivityDto> RecentActivity,
    int OverallProgressPercent);

public sealed record RecentActivityDto(
    string Kind,
    string Title,
    DateTime OccurredAt);
