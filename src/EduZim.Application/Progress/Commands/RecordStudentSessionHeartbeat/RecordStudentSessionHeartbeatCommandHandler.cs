using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Commands.RecordStudentSessionHeartbeat;

public sealed class RecordStudentSessionHeartbeatCommandHandler
    : IRequestHandler<RecordStudentSessionHeartbeatCommand, StudentSessionDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RecordStudentSessionHeartbeatCommandHandler> _logger;

    public RecordStudentSessionHeartbeatCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<RecordStudentSessionHeartbeatCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<StudentSessionDto> Handle(RecordStudentSessionHeartbeatCommand request, CancellationToken ct)
    {
        ScreenTimeAccess.EnsureCanOperateSession(_currentUser, request.TenantId, request.StudentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        int? limitSeconds = await LoadLimitAsync(request, ct).ConfigureAwait(false);
        List<StudentSession> sessions = await LoadSessionsAsync(request, ct).ConfigureAwait(false);
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        ScreenTimeSessionMaintenance.CloseStaleSessions(sessions, today, utcNow);
        StudentSession session = FindActive(sessions, request.StudentId, today);

        ApplyTick(session, sessions, limitSeconds, today, utcNow);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        int used = ScreenTimeLimitRules.UsedTodaySeconds(
            sessions.Select(StudentSessionMapper.ToSlice).ToList(),
            today,
            utcNow);
        if (session.Status == SessionStatus.Paused)
        {
            _logger.LogInformation(
                "Paused learning session for student {StudentId}; daily screen time limit reached.",
                request.StudentId);
        }

        return StudentSessionMapper.ToDto(session, used, limitSeconds);
    }

    private async Task<int?> LoadLimitAsync(RecordStudentSessionHeartbeatCommand request, CancellationToken ct)
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
        RecordStudentSessionHeartbeatCommand request,
        CancellationToken ct)
    {
        return await _db.StudentSessions
            .Where(s => s.TenantId == request.TenantId && s.StudentId == request.StudentId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static StudentSession FindActive(List<StudentSession> sessions, Guid studentId, DateOnly today)
    {
        StudentSession? session = sessions.FirstOrDefault(
            s => s.Status == SessionStatus.Active && s.SessionDate == today);
        if (session is null)
            throw new NotFoundException(nameof(StudentSession), studentId);

        return session;
    }

    private static void ApplyTick(
        StudentSession session,
        List<StudentSession> sessions,
        int? limitSeconds,
        DateOnly today,
        DateTime utcNow)
    {
        int usedByOthers = ScreenTimeLimitRules.UsedTodaySeconds(
            sessions.Where(s => s.Id != session.Id).Select(StudentSessionMapper.ToSlice).ToList(),
            today,
            utcNow);
        ScreenTimeLimitRules.TickResult tick = ScreenTimeLimitRules.Tick(
            session.AccumulatedSeconds,
            session.LastHeartbeatAt,
            utcNow,
            usedByOthers,
            limitSeconds);
        session.AccumulatedSeconds = tick.AccumulatedSeconds;
        session.LastHeartbeatAt = utcNow;
        session.UpdatedAt = utcNow;
        if (tick.Pause)
            session.Status = SessionStatus.Paused;
    }
}
