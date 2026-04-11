using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Content.Queries.GetCaptions;

public sealed class GetCaptionsQueryHandler
    : IRequestHandler<GetCaptionsQuery, IReadOnlyList<CaptionTrackSignedUrlDto>>
{
    private readonly IEduZimDbContext _db;
    private readonly IStorageService _storage;
    private readonly ContentStorageOptions _options;

    public GetCaptionsQueryHandler(
        IEduZimDbContext db,
        IStorageService storage,
        IOptions<ContentStorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<CaptionTrackSignedUrlDto>> Handle(
        GetCaptionsQuery request,
        CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var exists = await _db.ContentItems.AsNoTracking()
            .AnyAsync(c => c.Id == request.ContentId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(ContentItem), request.ContentId);

        var tracks = await _db.CaptionTracks.AsNoTracking()
            .Where(t => t.ContentItemId == request.ContentId)
            .OrderBy(t => t.Language)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ttl = TimeSpan.FromMinutes(_options.SignedUrlExpiryMinutes);
        var result = new List<CaptionTrackSignedUrlDto>(tracks.Count);
        foreach (var t in tracks)
        {
            var url = await _storage.GetSignedUrlAsync(t.StorageKey, ttl).ConfigureAwait(false);
            result.Add(new CaptionTrackSignedUrlDto(t.Id, t.Language, url));
        }

        return result;
    }
}
