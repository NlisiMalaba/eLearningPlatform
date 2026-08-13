using FluentValidation;

namespace EduZim.Application.Sync.Commands.ProcessOfflineQueue;

public sealed class ProcessOfflineQueueCommandValidator : AbstractValidator<ProcessOfflineQueueCommand>
{
    public const int MaxItemsPerUpload = 500;

    public ProcessOfflineQueueCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.Items).NotNull().Must(items => items.Count <= MaxItemsPerUpload)
            .WithMessage($"At most {MaxItemsPerUpload} items can be uploaded in one request.");
        RuleForEach(c => c.Items).SetValidator(new OfflineSyncItemDtoValidator());
    }
}
