using EduZim.Application.Common.Interfaces;
using EduZim.Application.LiveClassrooms.DTOs;
using EduZim.Application.LiveClassrooms.Hubs;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EduZim.Infrastructure.Hubs;

[Authorize]
public sealed class ClassroomHub : Hub
{
    private readonly IClassroomRealtimeService _realtime;
    private readonly IClassroomPresenceTracker _presence;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogWriter _audit;
    private readonly ILogger<ClassroomHub> _logger;

    public ClassroomHub(
        IClassroomRealtimeService realtime,
        IClassroomPresenceTracker presence,
        ICurrentUser currentUser,
        IAuditLogWriter audit,
        ILogger<ClassroomHub> logger)
    {
        _realtime = realtime;
        _presence = presence;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public Task JoinSession(Guid sessionId) =>
        RunAsync("JoinSession", sessionId, async () =>
        {
            ClassroomSession session = await _realtime
                .AuthorizeJoinAsync(sessionId, Context.ConnectionAborted)
                .ConfigureAwait(false);
            bool isFirstConnection = _presence.Join(session.Id, _currentUser.UserId, Context.ConnectionId);
            string group = ClassroomHubGroups.ForSession(session.Id);
            await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted)
                .ConfigureAwait(false);
            await Clients.Caller
                .SendAsync(
                    ClassroomHubEvents.PresenceSnapshot,
                    _presence.Snapshot(session.Id),
                    Context.ConnectionAborted)
                .ConfigureAwait(false);
            if (!isFirstConnection)
                return;

            await Clients.OthersInGroup(group)
                .SendAsync(
                    ClassroomHubEvents.ParticipantJoined,
                    new ClassroomParticipantPresenceDto(session.Id, _currentUser.UserId),
                    Context.ConnectionAborted)
                .ConfigureAwait(false);
            _logger.LogDebug("User joined classroom session {SessionId}.", session.Id);
        });

    public Task LeaveSession(Guid sessionId) =>
        RunAsync("LeaveSession", sessionId, async () =>
        {
            await Groups
                .RemoveFromGroupAsync(
                    Context.ConnectionId,
                    ClassroomHubGroups.ForSession(sessionId),
                    Context.ConnectionAborted)
                .ConfigureAwait(false);
            await BroadcastLeaveIfLastConnectionAsync().ConfigureAwait(false);
        });

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await BroadcastLeaveIfLastConnectionAsync().ConfigureAwait(false);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    public Task StartScreenShare(Guid sessionId) =>
        RunControlAsync(sessionId, async session =>
        {
            _presence.SetScreenShare(session.Id, _currentUser.UserId);
            await Clients.Group(ClassroomHubGroups.ForSession(session.Id))
                .SendAsync(
                    ClassroomHubEvents.ScreenShareStarted,
                    new ClassroomScreenShareDto(session.Id, _currentUser.UserId),
                    Context.ConnectionAborted)
                .ConfigureAwait(false);
            _logger.LogInformation("Screen share started for classroom session {SessionId}.", session.Id);
        });

    public Task StopScreenShare(Guid sessionId) =>
        RunControlAsync(sessionId, async session =>
        {
            _presence.SetScreenShare(session.Id, null);
            await Clients.Group(ClassroomHubGroups.ForSession(session.Id))
                .SendAsync(
                    ClassroomHubEvents.ScreenShareStopped,
                    new ClassroomScreenShareDto(session.Id, _currentUser.UserId),
                    Context.ConnectionAborted)
                .ConfigureAwait(false);
            _logger.LogInformation("Screen share stopped for classroom session {SessionId}.", session.Id);
        });

    public Task PresentContent(Guid sessionId, Guid contentItemId) =>
        RunAsync("PresentContent", sessionId, async () =>
        {
            ClassroomSession session = await _realtime
                .AuthorizePresentContentAsync(sessionId, contentItemId, Context.ConnectionAborted)
                .ConfigureAwait(false);
            _presence.SetPresentedContent(session.Id, contentItemId);
            await Clients.Group(ClassroomHubGroups.ForSession(session.Id))
                .SendAsync(
                    ClassroomHubEvents.ContentPresented,
                    new ClassroomContentPresentedDto(session.Id, contentItemId, _currentUser.UserId),
                    Context.ConnectionAborted)
                .ConfigureAwait(false);
            _logger.LogInformation("Content presented in classroom session {SessionId}.", session.Id);
        });

    public Task SetStudentAudio(Guid sessionId, Guid studentUserId, bool enabled) =>
        SetStudentMediaAsync(sessionId, studentUserId, audioEnabled: enabled, videoEnabled: null);

    public Task SetStudentVideo(Guid sessionId, Guid studentUserId, bool enabled) =>
        SetStudentMediaAsync(sessionId, studentUserId, audioEnabled: null, videoEnabled: enabled);

    private async Task SetStudentMediaAsync(
        Guid sessionId,
        Guid studentUserId,
        bool? audioEnabled,
        bool? videoEnabled)
    {
        await RunAsync("SetStudentMedia", sessionId, async () =>
        {
            ClassroomSession session = await _realtime
                .AuthorizeStudentMediaAsync(sessionId, studentUserId, Context.ConnectionAborted)
                .ConfigureAwait(false);
            ClassroomStudentMediaState current = _presence.GetStudentMedia(session.Id, studentUserId);
            bool audio = audioEnabled ?? current.AudioEnabled;
            bool video = videoEnabled ?? current.VideoEnabled;
            _presence.SetStudentMedia(session.Id, studentUserId, audio, video);
            await Clients.Group(ClassroomHubGroups.ForSession(session.Id))
                .SendAsync(
                    ClassroomHubEvents.StudentMediaChanged,
                    new ClassroomStudentMediaDto(session.Id, studentUserId, audio, video),
                    Context.ConnectionAborted)
                .ConfigureAwait(false);
            _logger.LogInformation("Student media updated in classroom session {SessionId}.", session.Id);
        }).ConfigureAwait(false);
    }

    private Task RunControlAsync(Guid sessionId, Func<ClassroomSession, Task> action) =>
        RunAsync("ControlSession", sessionId, async () =>
        {
            ClassroomSession session = await _realtime
                .AuthorizeControlAsync(sessionId, Context.ConnectionAborted)
                .ConfigureAwait(false);
            await action(session).ConfigureAwait(false);
        });

    private async Task BroadcastLeaveIfLastConnectionAsync()
    {
        if (!_presence.TryLeave(Context.ConnectionId, out Guid sessionId, out Guid userId, out bool userLeft))
            return;
        if (!userLeft)
            return;

        await Clients.Group(ClassroomHubGroups.ForSession(sessionId))
            .SendAsync(
                ClassroomHubEvents.ParticipantLeft,
                new ClassroomParticipantPresenceDto(sessionId, userId),
                Context.ConnectionAborted)
            .ConfigureAwait(false);
        _logger.LogDebug("User left classroom session {SessionId}.", sessionId);
    }

    private Task RunAsync(string action, Guid sessionId, Func<Task> work) =>
        ClassroomHubErrorGate.RunAsync(action, sessionId, _currentUser, _audit, Context, work);
}
