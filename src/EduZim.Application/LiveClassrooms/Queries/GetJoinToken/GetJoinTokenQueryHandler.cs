using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.LiveClassrooms.Queries.GetJoinToken;

public sealed class GetJoinTokenQueryHandler : IRequestHandler<GetJoinTokenQuery, JoinTokenDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IVideoService _video;
    private readonly ILogger<GetJoinTokenQueryHandler> _logger;

    public GetJoinTokenQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IVideoService video,
        ILogger<GetJoinTokenQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _video = video;
        _logger = logger;
    }

    public async Task<JoinTokenDto> Handle(GetJoinTokenQuery request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        ClassroomSession session = await LoadSessionAsync(request, ct).ConfigureAwait(false);
        if (session.SessionEndTime is not null)
            throw new ConflictException("This classroom session has already ended.");

        bool enrolled = await IsEnrolledAsync(request, session, ct).ConfigureAwait(false);
        LiveClassroomAccess.EnsureCanJoin(_currentUser, request.TenantId, session, enrolled);

        string token = await _video
            .GetJoinTokenAsync(session.RoomId, _currentUser.UserId.ToString(), ct)
            .ConfigureAwait(false);
        await RecordJoinAsync(request, session, ct).ConfigureAwait(false);

        _logger.LogDebug("Join token issued for session {SessionId}.", session.Id);
        return new JoinTokenDto(session.Id, session.RoomId, token);
    }

    private async Task<ClassroomSession> LoadSessionAsync(GetJoinTokenQuery request, CancellationToken ct)
    {
        ClassroomSession? session = await _db.ClassroomSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (session is null)
            throw new NotFoundException(nameof(ClassroomSession), request.SessionId);

        return session;
    }

    private async Task<bool> IsEnrolledAsync(
        GetJoinTokenQuery request,
        ClassroomSession session,
        CancellationToken ct)
    {
        if (_currentUser.Role != UserRole.Student)
            return false;

        return await _db.ClassEnrollments
            .AsNoTracking()
            .AnyAsync(
                e => e.TenantId == request.TenantId
                    && e.SchoolClassId == session.SchoolClassId
                    && e.StudentUserId == _currentUser.UserId,
                ct)
            .ConfigureAwait(false);
    }

    private async Task RecordJoinAsync(GetJoinTokenQuery request, ClassroomSession session, CancellationToken ct)
    {
        ClassroomParticipant? existing = await _db.ClassroomParticipants
            .FirstOrDefaultAsync(
                p => p.TenantId == request.TenantId
                    && p.ClassroomSessionId == session.Id
                    && p.UserId == _currentUser.UserId,
                ct)
            .ConfigureAwait(false);
        if (existing is not null)
            return;

        DateTime utcNow = DateTime.UtcNow;
        ClassroomParticipant row = new ClassroomParticipant
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ClassroomSessionId = session.Id,
            UserId = _currentUser.UserId,
            JoinedAtUtc = utcNow,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        await _db.ClassroomParticipants.AddAsync(row, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
