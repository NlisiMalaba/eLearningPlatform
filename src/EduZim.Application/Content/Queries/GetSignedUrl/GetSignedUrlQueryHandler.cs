using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Content.Queries.GetSignedUrl;

public sealed class GetSignedUrlQueryHandler : IRequestHandler<GetSignedUrlQuery, string>
{
    private readonly IEduZimDbContext _db;
    private readonly IStorageService _storage;
    private readonly ContentStorageOptions _options;

    public GetSignedUrlQueryHandler(
        IEduZimDbContext db,
        IStorageService storage,
        IOptions<ContentStorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<string> Handle(GetSignedUrlQuery request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var content = await _db.ContentItems.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ContentId, cancellationToken)
            .ConfigureAwait(false);
        if (content is null)
            throw new NotFoundException(nameof(ContentItem), request.ContentId);

        var ttl = request.UrlTtl ?? TimeSpan.FromMinutes(_options.SignedUrlExpiryMinutes);
        return await _storage.GetSignedUrlAsync(content.StorageKey, ttl).ConfigureAwait(false);
    }
}
