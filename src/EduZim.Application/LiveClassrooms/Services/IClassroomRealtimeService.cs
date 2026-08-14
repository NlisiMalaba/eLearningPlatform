using EduZim.Domain.Entities;

namespace EduZim.Application.LiveClassrooms.Services;

public interface IClassroomRealtimeService
{
    Task<ClassroomSession> AuthorizeJoinAsync(Guid sessionId, CancellationToken ct);

    Task<ClassroomSession> AuthorizeControlAsync(Guid sessionId, CancellationToken ct);

    Task<ClassroomSession> AuthorizePresentContentAsync(Guid sessionId, Guid contentItemId, CancellationToken ct);

    Task<ClassroomSession> AuthorizeStudentMediaAsync(Guid sessionId, Guid studentUserId, CancellationToken ct);
}
