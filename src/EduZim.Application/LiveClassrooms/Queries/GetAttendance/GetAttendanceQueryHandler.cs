using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.LiveClassrooms.Queries.GetAttendance;

public sealed class GetAttendanceQueryHandler : IRequestHandler<GetAttendanceQuery, SessionAttendanceDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetAttendanceQueryHandler> _logger;

    public GetAttendanceQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetAttendanceQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SessionAttendanceDto> Handle(GetAttendanceQuery request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        ClassroomSession session = await LoadSessionAsync(request, ct).ConfigureAwait(false);
        LiveClassroomAccess.EnsureCanViewAttendance(_currentUser, request.TenantId, session);

        List<AttendanceRecord> rows = await _db.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.TenantId == request.TenantId && a.ClassroomSessionId == request.SessionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Attendance queried for session {SessionId}: {Count} row(s).",
            session.Id,
            rows.Count);
        return AttendanceMapper.ToDto(session.Id, rows);
    }

    private async Task<ClassroomSession> LoadSessionAsync(GetAttendanceQuery request, CancellationToken ct)
    {
        ClassroomSession? session = await _db.ClassroomSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (session is null)
            throw new NotFoundException(nameof(ClassroomSession), request.SessionId);

        return session;
    }
}
