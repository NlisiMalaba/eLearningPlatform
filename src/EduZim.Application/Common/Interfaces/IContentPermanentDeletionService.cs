namespace EduZim.Application.Common.Interfaces;

public interface IContentPermanentDeletionService
{
    Task ExecuteAsync(Guid tenantId, Guid contentItemId, CancellationToken cancellationToken = default);
}
