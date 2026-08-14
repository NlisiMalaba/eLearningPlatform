using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Commands.RecordStudentInteraction;

public sealed class RecordStudentInteractionCommandHandler
    : IRequestHandler<RecordStudentInteractionCommand, StudentSessionDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RecordStudentInteractionCommandHandler> _logger;

    public RecordStudentInteractionCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<RecordStudentInteractionCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<StudentSessionDto> Handle(RecordStudentInteractionCommand request, CancellationToken ct)
    {
        ScreenTimeAccess.EnsureCanOperateSession(_currentUser, request.TenantId, request.StudentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        int? limitSeconds = await LoadLimitAsync(request, ct).ConfigureAwait(false);
        TenantTier? tier = await PreschoolSessionApplier
            .LoadTierAsync(_db, request.TenantId, ct)
            .ConfigureAwait(false);
        List<StudentSession> sessions = await LoadSessionsAsync(request, ct).ConfigureAwait(false);
        DateTime utcNow = DateTime.UtcNow;
        DateOnly today = ScreenTimeLimitRules.CalendarDay(utcNow);
        ScreenTimeSessionMaintenance.CloseStaleSessions(sessions, today, utcNow);
        StudentSession session = FindActive(sessions, request.StudentId, today);
        bool restAlreadyRequired = session.RestPromptRequired;

        PreschoolSessionApplier.ApplyInteraction(session, tier, utcNow);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (!restAlreadyRequired)
            LogRestPromptIfNeeded(session, request.StudentId);

        int used = ScreenTimeLimitRules.UsedTodaySeconds(
            sessions.Select(StudentSessionMapper.ToSlice).ToList(),
            today,
            utcNow);
        return StudentSessionMapper.ToDto(session, used, limitSeconds, utcNow);
    }

    private async Task<int?> LoadLimitAsync(RecordStudentInteractionCommand request, CancellationToken ct)
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
        RecordStudentInteractionCommand request,
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

    private void LogRestPromptIfNeeded(StudentSession session, Guid studentId)
    {
        if (!session.RestPromptRequired)
            return;

        _logger.LogInformation(
            "Rest prompt required for preschool session of student {StudentId} after {Seconds} seconds.",
            studentId,
            PreschoolSessionRules.RestPromptAfterSeconds);
    }
}
