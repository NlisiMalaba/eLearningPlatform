using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Content;

internal sealed class FakeStorageService : IStorageService
{
    public List<string> UploadedKeys { get; } = new();

    public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        UploadedKeys.Add(key);
        return Task.FromResult(key);
    }

    public Task<string> GetSignedUrlAsync(string key, TimeSpan expiry) =>
        Task.FromResult("https://test.invalid/" + Uri.EscapeDataString(key));

    public Task DeleteAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
}
