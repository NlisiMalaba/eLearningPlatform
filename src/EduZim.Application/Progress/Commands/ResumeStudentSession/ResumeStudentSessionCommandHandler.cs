using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Commands.ResumeStudentSession;

public sealed class ResumeStudentSessionCommandHandler
    : IRequestHandler<ResumeStudentSessionCommand, StudentSessionDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ResumeStudentSessionCommandHandler> _logger;

    public ResumeStudentSessionCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<ResumeStudentSessionCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<StudentSessionDto> Handle(ResumeStudentSessionCommand request, CancellationToken ct)
    {
        ScreenTimeAccess.EnsureCanOperateSession(_currentUser, request.TenantId, request.StudentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        int? limitSeconds = await LoadLimitAsync(request, ct).ConfigureAwait(false);
        List<StudentSession> sessions = await LoadSessionsAsync(request, ct).ConfigureAwait(false);
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        ScreenTimeSessionMaintenance.CloseStaleSessions(sessions, today, utcNow);
        StudentSession session = FindResumable(sessions, request.StudentId, today);
        EnsureNotBlockedByScreenTime(sessions, limitSeconds, today, utcNow);

        PreschoolSessionApplier.ApplyResume(session, utcNow);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Resumed learning session for student {StudentId}.", request.StudentId);
        int used = ScreenTimeLimitRules.UsedTodaySeconds(
            sessions.Select(StudentSessionMapper.ToSlice).ToList(),
            today,
            utcNow);
        return StudentSessionMapper.ToDto(session, used, limitSeconds, utcNow);
    }

    private async Task<int?> LoadLimitAsync(ResumeStudentSessionCommand request, CancellationToken ct)
    {
        ApplicationUser? student = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == request.StudentId
                    && u.TenantId == request.TenantId
                    && u.Role == UserRole.Student,
                ct)
            .ConfigureAwait(false);
        if (student is null)
            throw new NotFoundException(nameof(ApplicationUser), request.StudentId);

        return student.DailyScreenTimeLimitSeconds;
    }

    private async Task<List<StudentSession>> LoadSessionsAsync(
        ResumeStudentSessionCommand request,
        CancellationToken ct)
    {
        return await _db.StudentSessions
            .Where(s => s.TenantId == request.TenantId && s.StudentId == request.StudentId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static StudentSession FindResumable(List<StudentSession> sessions, Guid studentId, DateOnly today)
    {
        StudentSession? session = sessions.FirstOrDefault(
            s => s.SessionDate == today
                && (s.Status == SessionStatus.Paused || s.RestPromptRequired));
        if (session is null)
            throw new NotFoundException(nameof(StudentSession), studentId);

        return session;
    }

    private static void EnsureNotBlockedByScreenTime(
        List<StudentSession> sessions,
        int? limitSeconds,
        DateOnly today,
        DateTime utcNow)
    {
        int used = ScreenTimeLimitRules.UsedTodaySeconds(
            sessions.Select(StudentSessionMapper.ToSlice).ToList(),
            today,
            utcNow);
        if (!ScreenTimeLimitRules.IsLimitReached(limitSeconds, used))
            return;

        throw new ConflictException(
            "Daily screen time limit reached. The session cannot be resumed until the next calendar day.");
    }
}
