using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Content.Queries.GetTranscript;

public sealed class GetTranscriptQueryHandler : IRequestHandler<GetTranscriptQuery, TranscriptSignedUrlDto>
{
    private readonly IEduZimDbContext _db;
    private readonly IStorageService _storage;
    private readonly ContentStorageOptions _options;

    public GetTranscriptQueryHandler(
        IEduZimDbContext db,
        IStorageService storage,
        IOptions<ContentStorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<TranscriptSignedUrlDto> Handle(GetTranscriptQuery request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var contentExists = await _db.ContentItems.AsNoTracking()
            .AnyAsync(c => c.Id == request.ContentId, cancellationToken)
            .ConfigureAwait(false);
        if (!contentExists)
            throw new NotFoundException(nameof(ContentItem), request.ContentId);

        var transcript = await _db.Transcripts.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ContentItemId == request.ContentId, cancellationToken)
            .ConfigureAwait(false);
        if (transcript is null)
            throw new NotFoundException(nameof(Transcript), request.ContentId);

        var ttl = TimeSpan.FromMinutes(_options.SignedUrlExpiryMinutes);
        var url = await _storage.GetSignedUrlAsync(transcript.StorageKey, ttl).ConfigureAwait(false);
        return new TranscriptSignedUrlDto(transcript.Id, url);
    }
}
