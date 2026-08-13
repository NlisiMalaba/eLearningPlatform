using EduZim.Application.LiveClassrooms.DTOs;

namespace EduZim.Application.LiveClassrooms.Services;

public interface IClassroomPresenceTracker
{
    bool Join(Guid sessionId, Guid userId, string connectionId);

    bool TryLeave(string connectionId, out Guid sessionId, out Guid userId, out bool userLeftSession);

    IReadOnlyList<Guid> GetPresentUserIds(Guid sessionId);

    void SetScreenShare(Guid sessionId, Guid? userId);

    void SetPresentedContent(Guid sessionId, Guid? contentItemId);

    void SetStudentMedia(Guid sessionId, Guid studentUserId, bool audioEnabled, bool videoEnabled);

    ClassroomStudentMediaState GetStudentMedia(Guid sessionId, Guid studentUserId);

    ClassroomPresenceSnapshotDto Snapshot(Guid sessionId);
}
