namespace EduZim.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task<string> GetSignedUrlAsync(string key, TimeSpan expiry);
}
