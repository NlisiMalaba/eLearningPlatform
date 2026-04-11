using EduZim.Application.Common;
using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Content.Commands.UploadContent;

public sealed class UploadContentCommandHandler : IRequestHandler<UploadContentCommand, Guid>
{
    private readonly IEduZimDbContext _db;
    private readonly IStorageService _storage;
    private readonly ICurrentUser _currentUser;
    private readonly ContentStorageOptions _options;
    private readonly ILogger<UploadContentCommandHandler> _logger;

    public UploadContentCommandHandler(
        IEduZimDbContext db,
        IStorageService storage,
        ICurrentUser currentUser,
        IOptions<ContentStorageOptions> options,
        ILogger<UploadContentCommandHandler> logger)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Guid> Handle(UploadContentCommand request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var maxBytes = ContentLimits.MaxBytesFor(request.Type, _options);
        if (request.FileSizeBytes > maxBytes)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.FileSizeBytes)] =
                [
                    $"File size exceeds the maximum allowed for {request.Type} content ({maxBytes} bytes).",
                ],
            });
        }

        if (request.Content.CanSeek && request.Content.Length > maxBytes)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(request.FileSizeBytes)] =
                [
                    $"Stream length exceeds the maximum allowed for {request.Type} content ({maxBytes} bytes).",
                ],
            });
        }

        var id = Guid.NewGuid();
        var key = $"{request.TenantId}/content/{id}/{SanitizeFileSegment(request.Title)}";

        await _storage.UploadAsync(key, request.Content, request.ContentTypeHeader, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var entity = new ContentItem
        {
            Id = id,
            TenantId = request.TenantId,
            Title = request.Title,
            Type = request.Type,
            StorageKey = key,
            FileSizeBytes = request.FileSizeBytes,
            DurationSeconds = null,
            Language = request.Language,
            Status = ContentStatus.Draft,
            UploadedByUserId = _currentUser.UserId,
            ArchivedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _db.ContentItems.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Uploaded content {ContentId} for tenant {TenantId}, type {Type}, key {StorageKey}.",
            id,
            request.TenantId,
            request.Type,
            key);

        return id;
    }

    private static string SanitizeFileSegment(string title)
    {
        var trimmed = string.IsNullOrWhiteSpace(title) ? "file" : title.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            trimmed = trimmed.Replace(c, '_');
        return trimmed.Length > 200 ? trimmed[..200] : trimmed;
    }
}
