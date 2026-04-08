namespace EduZim.Application.Common.Interfaces;

public interface IVideoService
{
    Task<string> GetJoinTokenAsync(string roomId, string participantId, CancellationToken ct = default);
    Task<string?> GetRecordingUrlAsync(string roomId, CancellationToken ct = default);
}
