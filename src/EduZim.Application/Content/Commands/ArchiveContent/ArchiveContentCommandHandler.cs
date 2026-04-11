using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Content.Commands.ArchiveContent;

public sealed class ArchiveContentCommandHandler : IRequestHandler<ArchiveContentCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly IContentBackgroundJobs _backgroundJobs;
    private readonly ContentStorageOptions _options;
    private readonly ILogger<ArchiveContentCommandHandler> _logger;

    public ArchiveContentCommandHandler(
        IEduZimDbContext db,
        IContentBackgroundJobs backgroundJobs,
        IOptions<ContentStorageOptions> options,
        ILogger<ArchiveContentCommandHandler> logger)
    {
        _db = db;
        _backgroundJobs = backgroundJobs;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Unit> Handle(ArchiveContentCommand request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var content = await _db.ContentItems
            .FirstOrDefaultAsync(c => c.Id == request.ContentId, cancellationToken)
            .ConfigureAwait(false);
        if (content is null)
            throw new NotFoundException(nameof(ContentItem), request.ContentId);

        if (content.Status == ContentStatus.Archived)
            return Unit.Value;

        if (!string.IsNullOrEmpty(content.PermanentDeletionHangfireJobId))
            _backgroundJobs.TryCancelJob(content.PermanentDeletionHangfireJobId);

        var archivedAt = DateTime.UtcNow;
        content.Status = ContentStatus.Archived;
        content.ArchivedAt = archivedAt;
        content.UpdatedAt = archivedAt;

        var runAt = archivedAt.AddDays(_options.ArchivedRetentionDays);
        content.PermanentDeletionHangfireJobId =
            _backgroundJobs.SchedulePermanentDeletionAt(request.TenantId, content.Id, runAt);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Archived content {ContentId} for tenant {TenantId}; permanent deletion scheduled at {RunAtUtc} (job {JobId}).",
            content.Id,
            request.TenantId,
            runAt,
            content.PermanentDeletionHangfireJobId);

        return Unit.Value;
    }
}
