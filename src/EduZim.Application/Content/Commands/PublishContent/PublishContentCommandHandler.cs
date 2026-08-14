using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Queries.GetContentById;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Content.Commands.PublishContent;

public sealed class PublishContentCommandHandler
    : IRequestHandler<PublishContentCommand, ContentDetailDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IStorageService _storage;
    private readonly ContentStorageOptions _options;
    private readonly ILogger<PublishContentCommandHandler> _logger;

    public PublishContentCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IStorageService storage,
        IOptions<ContentStorageOptions> options,
        ILogger<PublishContentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ContentDetailDto> Handle(PublishContentCommand request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        ContentItem content = await LoadAsync(request, ct).ConfigureAwait(false);
        Publish(content);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Published content {ContentId} for tenant {TenantId}.", content.Id, request.TenantId);
        return await MapAsync(content).ConfigureAwait(false);
    }

    private async Task<ContentItem> LoadAsync(PublishContentCommand request, CancellationToken ct)
    {
        ContentItem? content = await _db.ContentItems
            .FirstOrDefaultAsync(c => c.Id == request.ContentId && c.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (content is null)
            throw new NotFoundException(nameof(ContentItem), request.ContentId);

        return content;
    }

    private static void Publish(ContentItem content)
    {
        if (content.Status == ContentStatus.Published)
            return;

        if (content.Status == ContentStatus.Archived)
            throw new ConflictException("Archived content cannot be published.");

        DateTime utcNow = DateTime.UtcNow;
        content.Status = ContentStatus.Published;
        content.UpdatedAt = utcNow;
    }

    private async Task<ContentDetailDto> MapAsync(ContentItem content)
    {
        TimeSpan ttl = TimeSpan.FromMinutes(_options.SignedUrlExpiryMinutes);
        string downloadUrl = await _storage.GetSignedUrlAsync(content.StorageKey, ttl).ConfigureAwait(false);
        return new ContentDetailDto
        {
            Id = content.Id,
            Title = content.Title,
            Type = content.Type,
            Status = content.Status,
            Language = content.Language,
            FileSizeBytes = content.FileSizeBytes,
            DurationSeconds = content.DurationSeconds,
            CreatedAtUtc = content.CreatedAt,
            UpdatedAtUtc = content.UpdatedAt,
            ArchivedAtUtc = content.ArchivedAt,
            DownloadUrl = downloadUrl,
        };
    }
}
