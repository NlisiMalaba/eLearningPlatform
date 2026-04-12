using System.Collections.Concurrent;
using System.Text.Json;
using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Assessments;

internal sealed class EphemeralCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(key, out byte[]? bytes) || bytes.Length == 0)
            return Task.FromResult<T?>(default);
        return Task.FromResult(JsonSerializer.Deserialize<T>(bytes, JsonOptions));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        _store[key] = bytes;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
