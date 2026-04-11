using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace EduZim.Infrastructure.Storage;

public sealed class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly ContentStorageOptions _options;

    public S3StorageService(IAmazonS3 s3, IOptions<ContentStorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var bucket = _options.BucketName
            ?? throw new InvalidOperationException("ContentStorage:BucketName is required for S3 storage.");

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };
        await _s3.PutObjectAsync(request, ct).ConfigureAwait(false);
        return key;
    }

    public Task<string> GetSignedUrlAsync(string key, TimeSpan expiry)
    {
        var bucket = _options.BucketName
            ?? throw new InvalidOperationException("ContentStorage:BucketName is required for S3 storage.");

        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
        });
        return Task.FromResult(url);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var bucket = _options.BucketName
            ?? throw new InvalidOperationException("ContentStorage:BucketName is required for S3 storage.");

        try
        {
            await _s3.DeleteObjectAsync(bucket, key, ct).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Key already absent
        }
    }
}
