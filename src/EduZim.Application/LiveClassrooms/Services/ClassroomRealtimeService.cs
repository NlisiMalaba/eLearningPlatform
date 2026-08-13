using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.LiveClassrooms.Services;

public sealed class ClassroomRealtimeService : IClassroomRealtimeService
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ClassroomRealtimeService(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ClassroomSession> AuthorizeJoinAsync(Guid sessionId, CancellationToken ct)
    {
        ClassroomSession session = await LoadActiveSessionAsync(sessionId, ct).ConfigureAwait(false);
        bool enrolled = await IsCurrentUserEnrolledAsync(session, ct).ConfigureAwait(false);
        LiveClassroomAccess.EnsureCanJoin(_currentUser, session.TenantId, session, enrolled);
        return session;
    }

    public async Task<ClassroomSession> AuthorizeControlAsync(Guid sessionId, CancellationToken ct)
    {
        ClassroomSession session = await LoadActiveSessionAsync(sessionId, ct).ConfigureAwait(false);
        LiveClassroomAccess.EnsureCanControl(_currentUser, session.TenantId, session);
        return session;
    }

    public async Task<ClassroomSession> AuthorizePresentContentAsync(
        Guid sessionId,
        Guid contentItemId,
        CancellationToken ct)
    {
        ClassroomSession session = await AuthorizeControlAsync(sessionId, ct).ConfigureAwait(false);
        bool exists = await _db.ContentItems
            .AsNoTracking()
            .AnyAsync(
                c => c.Id == contentItemId && c.TenantId == session.TenantId && c.ArchivedAt == null,
                ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(ContentItem), contentItemId);

        return session;
    }

    public async Task<ClassroomSession> AuthorizeStudentMediaAsync(
        Guid sessionId,
        Guid studentUserId,
        CancellationToken ct)
    {
        ClassroomSession session = await AuthorizeControlAsync(sessionId, ct).ConfigureAwait(false);
        if (studentUserId == Guid.Empty || studentUserId == session.TeacherUserId)
            throw new ConflictException("Media controls apply to enrolled students only.");

        bool enrolled = await _db.ClassEnrollments
            .AsNoTracking()
            .AnyAsync(
                e => e.TenantId == session.TenantId
                    && e.SchoolClassId == session.SchoolClassId
                    && e.StudentUserId == studentUserId,
                ct)
            .ConfigureAwait(false);
        if (!enrolled)
            throw new NotFoundException(nameof(ClassEnrollment), studentUserId);

        return session;
    }

    private async Task<ClassroomSession> LoadActiveSessionAsync(Guid sessionId, CancellationToken ct)
    {
        Guid tenantId = RequireTenant(sessionId);
        await _db.SetSessionTenantIdAsync(tenantId, ct).ConfigureAwait(false);
        ClassroomSession? session = await _db.ClassroomSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, ct)
            .ConfigureAwait(false);
        if (session is null)
            throw new NotFoundException(nameof(ClassroomSession), sessionId);
        if (session.SessionEndTime is not null)
            throw new ConflictException("This classroom session has already ended.");

        return session;
    }

    private async Task<bool> IsCurrentUserEnrolledAsync(ClassroomSession session, CancellationToken ct)
    {
        if (_currentUser.Role != UserRole.Student)
            return false;

        return await _db.ClassEnrollments
            .AsNoTracking()
            .AnyAsync(
                e => e.TenantId == session.TenantId
                    && e.SchoolClassId == session.SchoolClassId
                    && e.StudentUserId == _currentUser.UserId,
                ct)
            .ConfigureAwait(false);
    }

    private Guid RequireTenant(Guid sessionId)
    {
        if (_currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Authentication is required.");
        if (_currentUser.TenantId is not Guid tenantId)
        {
            throw new TenantAccessViolationException(
                "You do not have access to this tenant.",
                null,
                sessionId);
        }

        return tenantId;
    }
}
