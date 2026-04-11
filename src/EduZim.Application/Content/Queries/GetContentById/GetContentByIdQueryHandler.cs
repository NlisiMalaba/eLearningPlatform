using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Content.Queries.GetContentById;

public sealed class GetContentByIdQueryHandler : IRequestHandler<GetContentByIdQuery, ContentDetailDto>
{
    private readonly IEduZimDbContext _db;
    private readonly IStorageService _storage;
    private readonly ContentStorageOptions _options;

    public GetContentByIdQueryHandler(
        IEduZimDbContext db,
        IStorageService storage,
        IOptions<ContentStorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<ContentDetailDto> Handle(GetContentByIdQuery request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var content = await _db.ContentItems.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == request.ContentId && c.TenantId == request.TenantId,
                cancellationToken)
            .ConfigureAwait(false);
        if (content is null)
            throw new NotFoundException(nameof(ContentItem), request.ContentId);

        var ttl = TimeSpan.FromMinutes(_options.SignedUrlExpiryMinutes);
        var downloadUrl = await _storage.GetSignedUrlAsync(content.StorageKey, ttl).ConfigureAwait(false);

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
