namespace EduZim.Application.LiveClassrooms.DTOs;

public sealed record ClassroomParticipantPresenceDto(Guid SessionId, Guid UserId);

public sealed record ClassroomScreenShareDto(Guid SessionId, Guid UserId);

public sealed record ClassroomContentPresentedDto(Guid SessionId, Guid ContentItemId, Guid PresentedByUserId);

public sealed record ClassroomStudentMediaState(bool AudioEnabled, bool VideoEnabled);

public sealed record ClassroomStudentMediaDto(
    Guid SessionId,
    Guid StudentUserId,
    bool AudioEnabled,
    bool VideoEnabled);

public sealed record ClassroomPresenceSnapshotDto(
    Guid SessionId,
    IReadOnlyList<Guid> ParticipantUserIds,
    Guid? ScreenShareUserId,
    Guid? PresentedContentItemId,
    IReadOnlyList<ClassroomStudentMediaDto> StudentMedia);
