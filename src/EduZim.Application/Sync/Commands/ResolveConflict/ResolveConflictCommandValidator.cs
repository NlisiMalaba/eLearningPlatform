using EduZim.Application.Sync.Commands.ProcessOfflineQueue;
using FluentValidation;

namespace EduZim.Application.Sync.Commands.ResolveConflict;

public sealed class ResolveConflictCommandValidator : AbstractValidator<ResolveConflictCommand>
{
    public ResolveConflictCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.QueueItemId).NotEmpty();
        RuleFor(c => c.LocalTimestamp).NotEqual(default(DateTime));
        RuleFor(c => c.Payload).NotNull().SetValidator(new OfflineSyncPayloadDtoValidator());
    }
}
