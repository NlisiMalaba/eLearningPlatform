using EduZim.Application.Sync.DTOs;
using EduZim.Application.Sync.Services;
using FluentValidation;

namespace EduZim.Application.Sync.Commands.ProcessOfflineQueue;

public sealed class OfflineSyncPayloadDtoValidator : AbstractValidator<OfflineSyncPayloadDto>
{
    public OfflineSyncPayloadDtoValidator()
    {
        RuleFor(p => p.Kind).NotEmpty().Must(OfflineSyncKinds.IsKnown)
            .WithMessage("Kind must be ModuleProgress or AssessmentAttempt.");

        When(p => p.Kind == OfflineSyncKinds.ModuleProgress, () =>
        {
            RuleFor(p => p.ModuleId).NotEmpty();
            RuleFor(p => p.TimeOnTaskSeconds).GreaterThanOrEqualTo(0);
        });

        When(p => p.Kind == OfflineSyncKinds.AssessmentAttempt, () =>
        {
            RuleFor(p => p.AssessmentId).NotEmpty();
            RuleFor(p => p.ScorePercent).NotNull().InclusiveBetween(0, 100);
            RuleFor(p => p.TimeTakenSeconds).NotNull().GreaterThanOrEqualTo(0);
            RuleFor(p => p.SubmittedAt).NotEmpty();
        });
    }
}

public sealed class OfflineSyncItemDtoValidator : AbstractValidator<OfflineSyncItemDto>
{
    public OfflineSyncItemDtoValidator()
    {
        RuleFor(i => i.LocalTimestamp).NotEqual(default(DateTime));
        RuleFor(i => i.Payload).NotNull().SetValidator(new OfflineSyncPayloadDtoValidator());
    }
}
