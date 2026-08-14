using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Commands.SetDailyScreenTimeLimit;

public sealed class SetDailyScreenTimeLimitCommandHandler
    : IRequestHandler<SetDailyScreenTimeLimitCommand, ScreenTimeSettingsDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SetDailyScreenTimeLimitCommandHandler> _logger;

    public SetDailyScreenTimeLimitCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<SetDailyScreenTimeLimitCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ScreenTimeSettingsDto> Handle(SetDailyScreenTimeLimitCommand request, CancellationToken ct)
    {
        await ScreenTimeAccess
            .EnsureCanSetLimitAsync(_db, _currentUser, request.TenantId, request.StudentId, ct)
            .ConfigureAwait(false);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        ApplicationUser student = await LoadStudentAsync(request, ct).ConfigureAwait(false);
        student.DailyScreenTimeLimitSeconds = request.DailyScreenTimeLimitSeconds;
        await PauseIfLimitAlreadyReachedAsync(request, student.DailyScreenTimeLimitSeconds, ct)
            .ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Updated daily screen time limit for student {StudentId}.", request.StudentId);
        return new ScreenTimeSettingsDto(request.StudentId, student.DailyScreenTimeLimitSeconds);
    }

    private async Task<ApplicationUser> LoadStudentAsync(SetDailyScreenTimeLimitCommand request, CancellationToken ct)
    {
        ApplicationUser? student = await _db.Users
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

    private async Task PauseIfLimitAlreadyReachedAsync(
        SetDailyScreenTimeLimitCommand request,
        int? limitSeconds,
        CancellationToken ct)
    {
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        List<StudentSession> sessions = await _db.StudentSessions
            .Where(s => s.TenantId == request.TenantId && s.StudentId == request.StudentId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (sessions.Count == 0)
            return;

        int used = ScreenTimeLimitRules.UsedTodaySeconds(
            sessions.Select(StudentSessionMapper.ToSlice).ToList(),
            today,
            utcNow);
        if (!ScreenTimeLimitRules.IsLimitReached(limitSeconds, used))
            return;

        foreach (StudentSession session in sessions)
        {
            if (session.Status == SessionStatus.Active && session.SessionDate == today)
            {
                session.Status = SessionStatus.Paused;
                session.UpdatedAt = utcNow;
            }
        }
    }
}
