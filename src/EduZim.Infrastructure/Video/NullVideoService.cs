using EduZim.Application.Common.Interfaces;
using EduZim.Infrastructure.Http;
using System.Net.Http.Json;

namespace EduZim.Infrastructure.Video;

public sealed class NullVideoService : IVideoService
{
    private readonly IHttpClientFactory _httpClients;

    public NullVideoService(IHttpClientFactory httpClients)
    {
        _httpClients = httpClients;
    }

    public async Task<string> GetJoinTokenAsync(string roomId, string participantId, CancellationToken ct = default)
    {
        HttpClient client = _httpClients.CreateClient(ExternalHttpClientNames.Video);
        if (!ExternalHttpCall.HasBaseAddress(client))
            return $"local-join:{roomId}:{participantId}";

        try
        {
            using HttpResponseMessage response = await client
                .PostAsJsonAsync($"rooms/{roomId}/tokens", new { participantId }, ct)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string token = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(token) ? $"local-join:{roomId}:{participantId}" : token.Trim('"');
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ExternalHttpCall.IsTransient(ex))
        {
            return $"local-join:{roomId}:{participantId}";
        }
    }

    public async Task<string?> GetRecordingUrlAsync(string roomId, CancellationToken ct = default)
    {
        HttpClient client = _httpClients.CreateClient(ExternalHttpClientNames.Video);
        if (!ExternalHttpCall.HasBaseAddress(client))
            return $"https://recordings.local/{roomId}";

        try
        {
            using HttpResponseMessage response = await client
                .GetAsync($"rooms/{roomId}/recording", ct)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            string url = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(url) ? null : url.Trim('"');
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ExternalHttpCall.IsTransient(ex))
        {
            return null;
        }
    }
}
