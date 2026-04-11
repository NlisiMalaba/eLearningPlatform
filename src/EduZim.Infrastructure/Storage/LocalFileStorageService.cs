using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace EduZim.Infrastructure.Storage;

public sealed class LocalFileStorageService : IStorageService
{
    private readonly ContentStorageOptions _options;
    private readonly string _rootDirectory;

    public LocalFileStorageService(IOptions<ContentStorageOptions> options)
    {
        _options = options.Value;
        _rootDirectory = string.IsNullOrWhiteSpace(_options.LocalRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "storage")
            : _options.LocalRootPath;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var path = PhysicalPath(key);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920,
            FileOptions.Asynchronous);
        await content.CopyToAsync(fs, ct).ConfigureAwait(false);
        return key;
    }

    public Task<string> GetSignedUrlAsync(string key, TimeSpan expiry)
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicDownloadBaseUrl))
        {
            var baseUrl = _options.PublicDownloadBaseUrl.TrimEnd('/');
            return Task.FromResult($"{baseUrl}/{Uri.EscapeDataString(key)}");
        }

        var path = PhysicalPath(key);
        return Task.FromResult(new Uri(path).AbsoluteUri);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = PhysicalPath(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string PhysicalPath(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(key))
            throw new ArgumentException("Invalid storage key.", nameof(key));

        var safeKey = key.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_rootDirectory, safeKey));
        var root = Path.GetFullPath(_rootDirectory);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage key resolves outside the configured root.");
        return full;
    }
}
