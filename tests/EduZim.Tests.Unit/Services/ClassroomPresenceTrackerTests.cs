using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Application.LiveClassrooms.Services;

namespace EduZim.Tests.Unit.Services;

public sealed class ClassroomPresenceTrackerTests
{
    [Fact]
    public void Tracks_at_least_fifty_concurrent_participants()
    {
        ClassroomPresenceTracker tracker = new();
        Guid sessionId = Guid.NewGuid();
        int count = ClassroomSessionLimits.MinSupportedConcurrentParticipants;
        for (int i = 0; i < count; i++)
            tracker.Join(sessionId, Guid.NewGuid(), Guid.NewGuid().ToString("N"));

        Assert.Equal(count, tracker.GetPresentUserIds(sessionId).Count);
        Assert.Equal(count, tracker.Snapshot(sessionId).ParticipantUserIds.Count);
    }

    [Fact]
    public void Rejoin_on_a_new_connection_does_not_duplicate_the_user()
    {
        ClassroomPresenceTracker tracker = new();
        Guid sessionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        bool first = tracker.Join(sessionId, userId, "conn-a");
        bool second = tracker.Join(sessionId, userId, "conn-b");

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(userId, Assert.Single(tracker.GetPresentUserIds(sessionId)));
    }

    [Fact]
    public void Leaving_one_connection_keeps_the_user_present()
    {
        ClassroomPresenceTracker tracker = new();
        Guid sessionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        tracker.Join(sessionId, userId, "conn-a");
        tracker.Join(sessionId, userId, "conn-b");

        bool left = tracker.TryLeave("conn-a", out Guid leftSession, out Guid leftUser, out bool userLeft);

        Assert.True(left);
        Assert.Equal(sessionId, leftSession);
        Assert.Equal(userId, leftUser);
        Assert.False(userLeft);
        Assert.Equal(userId, Assert.Single(tracker.GetPresentUserIds(sessionId)));
    }

    [Fact]
    public void Leaving_last_connection_removes_the_user()
    {
        ClassroomPresenceTracker tracker = new();
        Guid sessionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        tracker.Join(sessionId, userId, "conn-a");

        bool left = tracker.TryLeave("conn-a", out _, out _, out bool userLeft);

        Assert.True(left);
        Assert.True(userLeft);
        Assert.Empty(tracker.GetPresentUserIds(sessionId));
    }

    [Fact]
    public void Snapshot_includes_screen_share_and_media_overrides()
    {
        ClassroomPresenceTracker tracker = new();
        Guid sessionId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid contentId = Guid.NewGuid();
        tracker.Join(sessionId, teacherId, "t1");
        tracker.SetScreenShare(sessionId, teacherId);
        tracker.SetPresentedContent(sessionId, contentId);
        tracker.SetStudentMedia(sessionId, studentId, false, true);

        ClassroomPresenceSnapshotDto snapshot = tracker.Snapshot(sessionId);

        Assert.Equal(teacherId, snapshot.ScreenShareUserId);
        Assert.Equal(contentId, snapshot.PresentedContentItemId);
        ClassroomStudentMediaDto media = Assert.Single(snapshot.StudentMedia);
        Assert.Equal(studentId, media.StudentUserId);
        Assert.False(media.AudioEnabled);
        Assert.True(media.VideoEnabled);
        ClassroomStudentMediaState defaults = tracker.GetStudentMedia(sessionId, Guid.NewGuid());
        Assert.True(defaults.AudioEnabled);
        Assert.True(defaults.VideoEnabled);
    }
}
