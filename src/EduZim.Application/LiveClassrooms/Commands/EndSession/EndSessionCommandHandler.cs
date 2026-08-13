using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.LiveClassrooms.Commands.EndSession;

public sealed class EndSessionCommandHandler : IRequestHandler<EndSessionCommand, SessionAttendanceDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IVideoService _video;
    private readonly ILogger<EndSessionCommandHandler> _logger;

    public EndSessionCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IVideoService video,
        ILogger<EndSessionCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _video = video;
        _logger = logger;
    }

    public async Task<SessionAttendanceDto> Handle(EndSessionCommand request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        ClassroomSession session = await LoadSessionAsync(request, ct).ConfigureAwait(false);
        LiveClassroomAccess.EnsureCanEnd(_currentUser, request.TenantId, session);
        if (session.SessionEndTime is not null)
            throw new ConflictException("This classroom session has already ended.");

        DateTime utcNow = DateTime.UtcNow;
        session.SessionEndTime = utcNow;
        session.UpdatedAt = utcNow;
        session.RecordingUrl = await _video.GetRecordingUrlAsync(session.RoomId, ct).ConfigureAwait(false);

        List<AttendanceRecord> records = await CreateAttendanceAsync(request, session, utcNow, ct)
            .ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Ended classroom session {SessionId} with {Count} attendance record(s).",
            session.Id,
            records.Count);
        return AttendanceMapper.ToDto(session.Id, records);
    }

    private async Task<ClassroomSession> LoadSessionAsync(EndSessionCommand request, CancellationToken ct)
    {
        ClassroomSession? session = await _db.ClassroomSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (session is null)
            throw new NotFoundException(nameof(ClassroomSession), request.SessionId);

        return session;
    }

    private async Task<List<AttendanceRecord>> CreateAttendanceAsync(
        EndSessionCommand request,
        ClassroomSession session,
        DateTime endedAtUtc,
        CancellationToken ct)
    {
        List<ClassroomParticipant> joined = await _db.ClassroomParticipants
            .AsNoTracking()
            .Where(p => p.TenantId == request.TenantId && p.ClassroomSessionId == session.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        HashSet<Guid> studentIds = await LoadStudentIdsAsync(request, session, ct).ConfigureAwait(false);
        List<AttendanceRecord> records = [];
        foreach (ClassroomParticipant participant in joined)
        {
            if (!studentIds.Contains(participant.UserId))
                continue;

            AttendanceRecord row = BuildAttendance(request.TenantId, session.Id, participant, endedAtUtc);
            records.Add(row);
            await _db.AttendanceRecords.AddAsync(row, ct).ConfigureAwait(false);
        }

        return records;
    }

    private async Task<HashSet<Guid>> LoadStudentIdsAsync(
        EndSessionCommand request,
        ClassroomSession session,
        CancellationToken ct)
    {
        List<Guid> ids = await _db.ClassEnrollments
            .AsNoTracking()
            .Where(e => e.TenantId == request.TenantId && e.SchoolClassId == session.SchoolClassId)
            .Select(e => e.StudentUserId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return ids.ToHashSet();
    }

    private static AttendanceRecord BuildAttendance(
        Guid tenantId,
        Guid sessionId,
        ClassroomParticipant participant,
        DateTime endedAtUtc)
    {
        int duration = (int)Math.Max(0, (endedAtUtc - participant.JoinedAtUtc).TotalSeconds);
        DateTime utcNow = DateTime.UtcNow;
        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassroomSessionId = sessionId,
            StudentUserId = participant.UserId,
            JoinTimeUtc = participant.JoinedAtUtc,
            DurationSeconds = duration,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }
}
