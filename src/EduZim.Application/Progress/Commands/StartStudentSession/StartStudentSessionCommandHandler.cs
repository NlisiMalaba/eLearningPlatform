using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Commands.StartStudentSession;

public sealed class StartStudentSessionCommandHandler
    : IRequestHandler<StartStudentSessionCommand, StudentSessionDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<StartStudentSessionCommandHandler> _logger;

    public StartStudentSessionCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<StartStudentSessionCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<StudentSessionDto> Handle(StartStudentSessionCommand request, CancellationToken ct)
    {
        ScreenTimeAccess.EnsureCanOperateSession(_currentUser, request.TenantId, request.StudentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        ApplicationUser student = await LoadStudentAsync(request, ct).ConfigureAwait(false);
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        List<StudentSession> sessions = await LoadSessionsAsync(request, ct).ConfigureAwait(false);
        ScreenTimeSessionMaintenance.CloseStaleSessions(sessions, today, utcNow);

        StudentSession? active = sessions.FirstOrDefault(
            s => s.Status == SessionStatus.Active && s.SessionDate == today);
        if (active is not null)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Map(active, sessions, student.DailyScreenTimeLimitSeconds, today, utcNow);
        }

        EnsureUnderLimit(sessions, student.DailyScreenTimeLimitSeconds, today, utcNow);
        StudentSession created = await CreateSessionAsync(request, utcNow, ct).ConfigureAwait(false);
        sessions.Add(created);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Started learning session for student {StudentId}.", request.StudentId);
        return Map(created, sessions, student.DailyScreenTimeLimitSeconds, today, utcNow);
    }

    private async Task<ApplicationUser> LoadStudentAsync(StartStudentSessionCommand request, CancellationToken ct)
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

        return student;
    }

    private async Task<List<StudentSession>> LoadSessionsAsync(StartStudentSessionCommand request, CancellationToken ct)
    {
        return await _db.StudentSessions
            .Where(s => s.TenantId == request.TenantId && s.StudentId == request.StudentId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static void EnsureUnderLimit(
        List<StudentSession> sessions,
        int? limitSeconds,
        DateOnly today,
        DateTime utcNow)
    {
        int used = ScreenTimeLimitRules.UsedTodaySeconds(
            sessions.Select(StudentSessionMapper.ToSlice).ToList(),
            today,
            utcNow);
        if (ScreenTimeLimitRules.CanCreateSession(limitSeconds, used))
            return;

        throw new ConflictException(
            "Daily screen time limit reached. A new session cannot be started until the next calendar day.");
    }

    private async Task<StudentSession> CreateSessionAsync(
        StartStudentSessionCommand request,
        DateTime utcNow,
        CancellationToken ct)
    {
        StudentSession created = StudentSessionFactory.Create(request.TenantId, request.StudentId, utcNow);
        await _db.StudentSessions.AddAsync(created, ct).ConfigureAwait(false);
        return created;
    }

    private static StudentSessionDto Map(
        StudentSession session,
        List<StudentSession> sessions,
        int? limitSeconds,
        DateOnly today,
        DateTime utcNow)
    {
        int used = ScreenTimeLimitRules.UsedTodaySeconds(
            sessions.Select(StudentSessionMapper.ToSlice).ToList(),
            today,
            utcNow);
        return StudentSessionMapper.ToDto(session, used, limitSeconds, utcNow);
    }
}
