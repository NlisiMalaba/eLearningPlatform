namespace EduZim.Application.LiveClassrooms.DTOs;

public sealed record JoinTokenDto(Guid SessionId, string RoomId, string Token);
