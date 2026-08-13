namespace EduZim.Application.LiveClassrooms.Hubs;

public static class ClassroomHubEvents
{
    public const string ParticipantJoined = "ParticipantJoined";
    public const string ParticipantLeft = "ParticipantLeft";
    public const string PresenceSnapshot = "PresenceSnapshot";
    public const string ScreenShareStarted = "ScreenShareStarted";
    public const string ScreenShareStopped = "ScreenShareStopped";
    public const string ContentPresented = "ContentPresented";
    public const string StudentMediaChanged = "StudentMediaChanged";
}

public static class ClassroomHubGroups
{
    public static string ForSession(Guid sessionId) => $"classroom:{sessionId:N}";
}
