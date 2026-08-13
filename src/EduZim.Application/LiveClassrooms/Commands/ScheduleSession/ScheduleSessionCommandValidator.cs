using FluentValidation;

namespace EduZim.Application.LiveClassrooms.Commands.ScheduleSession;

public sealed class ScheduleSessionCommandValidator : AbstractValidator<ScheduleSessionCommand>
{
    public ScheduleSessionCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.SchoolClassId).NotEmpty();
        RuleFor(c => c.StartAtUtc).Must(d => d > DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("Start time must be in the future.");
        RuleFor(c => c.DurationMinutes).InclusiveBetween(1, 480);
    }
}
