using System.Collections.Concurrent;
using EduZim.Application.LiveClassrooms.DTOs;

namespace EduZim.Application.LiveClassrooms.Services;

public sealed class ClassroomPresenceTracker : IClassroomPresenceTracker
{
    private readonly ConcurrentDictionary<string, PresenceBinding> _byConnection = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, SessionLiveState> _sessions = new();

    public bool Join(Guid sessionId, Guid userId, string connectionId)
    {
        if (_byConnection.TryGetValue(connectionId, out PresenceBinding? existing)
            && existing.SessionId != sessionId)
        {
            TryLeave(connectionId, out _, out _, out _);
        }

        SessionLiveState state = _sessions.GetOrAdd(sessionId, static _ => new SessionLiveState());
        ConcurrentDictionary<string, byte> connections = state.ConnectionsByUser.GetOrAdd(
            userId,
            static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
        bool added = connections.TryAdd(connectionId, 0);
        _byConnection[connectionId] = new PresenceBinding(sessionId, userId);
        return added && connections.Count == 1;
    }

    public bool TryLeave(string connectionId, out Guid sessionId, out Guid userId, out bool userLeftSession)
    {
        sessionId = Guid.Empty;
        userId = Guid.Empty;
        userLeftSession = false;
        if (!_byConnection.TryRemove(connectionId, out PresenceBinding? binding))
            return false;

        sessionId = binding.SessionId;
        userId = binding.UserId;
        if (!_sessions.TryGetValue(sessionId, out SessionLiveState? state))
            return true;

        userLeftSession = RemoveUserConnection(state, userId, connectionId);
        if (state.ConnectionsByUser.IsEmpty)
            _sessions.TryRemove(sessionId, out _);
        return true;
    }

    public IReadOnlyList<Guid> GetPresentUserIds(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out SessionLiveState? state))
            return [];
        return state.ConnectionsByUser.Keys.ToList();
    }

    public void SetScreenShare(Guid sessionId, Guid? userId) =>
        State(sessionId).ScreenShareUserId = userId;

    public void SetPresentedContent(Guid sessionId, Guid? contentItemId) =>
        State(sessionId).PresentedContentItemId = contentItemId;

    public void SetStudentMedia(Guid sessionId, Guid studentUserId, bool audioEnabled, bool videoEnabled) =>
        State(sessionId).StudentMedia[studentUserId] = new ClassroomStudentMediaState(audioEnabled, videoEnabled);

    public ClassroomStudentMediaState GetStudentMedia(Guid sessionId, Guid studentUserId)
    {
        if (_sessions.TryGetValue(sessionId, out SessionLiveState? state)
            && state.StudentMedia.TryGetValue(studentUserId, out ClassroomStudentMediaState? media))
        {
            return media;
        }

        return new ClassroomStudentMediaState(true, true);
    }

    public ClassroomPresenceSnapshotDto Snapshot(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out SessionLiveState? state))
        {
            return new ClassroomPresenceSnapshotDto(sessionId, [], null, null, []);
        }

        IReadOnlyList<ClassroomStudentMediaDto> media = state.StudentMedia
            .Select(pair => new ClassroomStudentMediaDto(
                sessionId,
                pair.Key,
                pair.Value.AudioEnabled,
                pair.Value.VideoEnabled))
            .ToList();
        return new ClassroomPresenceSnapshotDto(
            sessionId,
            state.ConnectionsByUser.Keys.ToList(),
            state.ScreenShareUserId,
            state.PresentedContentItemId,
            media);
    }

    private SessionLiveState State(Guid sessionId) =>
        _sessions.GetOrAdd(sessionId, static _ => new SessionLiveState());

    private static bool RemoveUserConnection(SessionLiveState state, Guid userId, string connectionId)
    {
        if (!state.ConnectionsByUser.TryGetValue(userId, out ConcurrentDictionary<string, byte>? connections))
            return true;

        connections.TryRemove(connectionId, out _);
        if (!connections.IsEmpty)
            return false;

        state.ConnectionsByUser.TryRemove(userId, out _);
        return true;
    }

    private sealed record PresenceBinding(Guid SessionId, Guid UserId);

    private sealed class SessionLiveState
    {
        public ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> ConnectionsByUser { get; } = new();
        public Guid? ScreenShareUserId { get; set; }
        public Guid? PresentedContentItemId { get; set; }
        public ConcurrentDictionary<Guid, ClassroomStudentMediaState> StudentMedia { get; } = new();
    }
}
