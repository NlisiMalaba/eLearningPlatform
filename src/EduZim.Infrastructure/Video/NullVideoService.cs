using EduZim.Application.Common.Interfaces;

namespace EduZim.Infrastructure.Video;

public sealed class NullVideoService : IVideoService
{
    public Task<string> GetJoinTokenAsync(string roomId, string participantId, CancellationToken ct = default)
    {
        return Task.FromResult($"local-join:{roomId}:{participantId}");
    }

    public Task<string?> GetRecordingUrlAsync(string roomId, CancellationToken ct = default)
    {
        return Task.FromResult<string?>($"https://recordings.local/{roomId}");
    }
}
