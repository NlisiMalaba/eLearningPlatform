using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.LiveClassrooms.Queries.GetRecordingUrl;

public sealed class GetRecordingUrlQueryHandler : IRequestHandler<GetRecordingUrlQuery, string?>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetRecordingUrlQueryHandler> _logger;

    public GetRecordingUrlQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetRecordingUrlQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<string?> Handle(GetRecordingUrlQuery request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        ClassroomSession session = await LoadSessionAsync(request, ct).ConfigureAwait(false);
        bool enrolled = await IsEnrolledAsync(request, session, ct).ConfigureAwait(false);
        LiveClassroomAccess.EnsureCanViewRecording(_currentUser, request.TenantId, session, enrolled);

        if (session.SessionEndTime is not DateTime endedAt)
            throw new NotFoundException(nameof(ClassroomSession.RecordingUrl), request.SessionId);

        if (!RecordingAvailabilityRules.IsAccessible(endedAt, DateTime.UtcNow))
        {
            _logger.LogDebug("Recording expired for session {SessionId}.", session.Id);
            return null;
        }

        return session.RecordingUrl;
    }

    private async Task<ClassroomSession> LoadSessionAsync(GetRecordingUrlQuery request, CancellationToken ct)
    {
        ClassroomSession? session = await _db.ClassroomSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (session is null)
            throw new NotFoundException(nameof(ClassroomSession), request.SessionId);

        return session;
    }

    private async Task<bool> IsEnrolledAsync(
        GetRecordingUrlQuery request,
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
}
