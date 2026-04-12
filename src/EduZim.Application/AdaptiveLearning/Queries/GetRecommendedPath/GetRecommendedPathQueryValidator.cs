using FluentValidation;

namespace EduZim.Application.AdaptiveLearning.Queries.GetRecommendedPath;

public sealed class GetRecommendedPathQueryValidator : AbstractValidator<GetRecommendedPathQuery>
{
    public GetRecommendedPathQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.StudentId).NotEmpty();
        RuleFor(q => q.ModuleId).NotEmpty();
    }
}
