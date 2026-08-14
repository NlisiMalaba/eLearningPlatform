using EduZim.Application.Sync.DTOs;
using EduZim.Domain.Exceptions;
using System.Text.Json;

namespace EduZim.Application.Sync.Services;

internal static class OfflineSyncPayloadJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string Serialize(OfflineSyncPayloadDto payload) =>
        JsonSerializer.Serialize(payload, Options);

    public static OfflineSyncPayloadDto Deserialize(string json)
    {
        OfflineSyncPayloadDto? parsed = JsonSerializer.Deserialize<OfflineSyncPayloadDto>(json, Options);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Kind))
            throw new DomainException("Offline sync payload is invalid.");

        return parsed;
    }
}
