using FluentValidation;

namespace EduZim.Application.AdaptiveLearning.Queries.GetAdjustedDifficulty;

public sealed class GetAdjustedDifficultyQueryValidator : AbstractValidator<GetAdjustedDifficultyQuery>
{
    public GetAdjustedDifficultyQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.StudentId).NotEmpty();
        RuleFor(q => q.ModuleId).NotEmpty();
    }
}
