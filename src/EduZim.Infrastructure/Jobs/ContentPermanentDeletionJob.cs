using EduZim.Application.Common.Interfaces;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Hangfire entry point for permanent deletion of archived content after retention.</summary>
public sealed class ContentPermanentDeletionJob
{
    private readonly IContentPermanentDeletionService _deletion;

    public ContentPermanentDeletionJob(IContentPermanentDeletionService deletion)
    {
        _deletion = deletion;
    }

    public Task RunAsync(Guid tenantId, Guid contentItemId) =>
        _deletion.ExecuteAsync(tenantId, contentItemId, CancellationToken.None);
}
