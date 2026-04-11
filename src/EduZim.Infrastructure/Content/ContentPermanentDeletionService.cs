using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Infrastructure.Content;

public sealed class ContentPermanentDeletionService : IContentPermanentDeletionService
{
    private readonly EduZimDbContext _db;
    private readonly IStorageService _storage;
    private readonly ILogger<ContentPermanentDeletionService> _logger;

    public ContentPermanentDeletionService(
        EduZimDbContext db,
        IStorageService storage,
        ILogger<ContentPermanentDeletionService> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid tenantId, Guid contentItemId, CancellationToken cancellationToken = default)
    {
        await _db.SetSessionTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var content = await _db.ContentItems
            .Include(c => c.CaptionTracks)
            .Include(c => c.Transcript)
            .FirstOrDefaultAsync(c => c.Id == contentItemId && c.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (content is null)
        {
            _logger.LogWarning(
                "Content permanent deletion skipped: content {ContentId} not found for tenant {TenantId}.",
                contentItemId,
                tenantId);
            return;
        }

        if (content.Status != ContentStatus.Archived)
        {
            _logger.LogInformation(
                "Content permanent deletion skipped: content {ContentId} is not archived (status {Status}).",
                contentItemId,
                content.Status);
            return;
        }

        _logger.LogWarning(
            "Starting permanent deletion for archived content {ContentId}, tenant {TenantId}.",
            contentItemId,
            tenantId);

        await DeleteBlobIfPresentAsync(content.StorageKey, cancellationToken).ConfigureAwait(false);
        foreach (var track in content.CaptionTracks)
            await DeleteBlobIfPresentAsync(track.StorageKey, cancellationToken).ConfigureAwait(false);
        if (content.Transcript is not null)
            await DeleteBlobIfPresentAsync(content.Transcript.StorageKey, cancellationToken).ConfigureAwait(false);

        await _db.ModuleContentItems.Where(m => m.ContentItemId == contentItemId && m.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await _db.ContentItems.Where(c => c.Id == contentItemId && c.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogWarning("Completed permanent deletion for content {ContentId}, tenant {TenantId}.", contentItemId, tenantId);
    }

    private async Task DeleteBlobIfPresentAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete storage object {StorageKey}; continuing with DB cleanup.", key);
        }
    }
}
