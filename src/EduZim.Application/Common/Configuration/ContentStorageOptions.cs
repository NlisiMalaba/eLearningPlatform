namespace EduZim.Application.Common.Configuration;

public sealed class ContentStorageOptions
{
    public const string SectionName = "ContentStorage";

    /// <summary>When set, objects are stored in S3; otherwise <see cref="LocalRootPath"/> is used.</summary>
    public string? BucketName { get; set; }

    public string? Region { get; set; }

    /// <summary>Directory root for local file storage when <see cref="BucketName"/> is not set.</summary>
    public string? LocalRootPath { get; set; }

    /// <summary>Optional public base URL for local signed URLs (no trailing slash).</summary>
    public string? PublicDownloadBaseUrl { get; set; }

    public long MaxVideoBytes { get; set; } = 500L * 1024 * 1024;
    public long MaxAudioBytes { get; set; } = 50L * 1024 * 1024;
    public long MaxOtherBytes { get; set; } = 100L * 1024 * 1024;

    public int ArchivedRetentionDays { get; set; } = 30;
    public int SignedUrlExpiryMinutes { get; set; } = 60;
}
