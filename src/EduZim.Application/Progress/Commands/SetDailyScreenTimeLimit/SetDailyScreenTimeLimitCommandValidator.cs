using EduZim.Application.Progress.Services;
using FluentValidation;

namespace EduZim.Application.Progress.Commands.SetDailyScreenTimeLimit;

public sealed class SetDailyScreenTimeLimitCommandValidator : AbstractValidator<SetDailyScreenTimeLimitCommand>
{
    public SetDailyScreenTimeLimitCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.DailyScreenTimeLimitSeconds)
            .InclusiveBetween(ScreenTimeLimitRules.MinLimitSeconds, ScreenTimeLimitRules.MaxLimitSeconds)
            .When(c => c.DailyScreenTimeLimitSeconds.HasValue);
    }
}
