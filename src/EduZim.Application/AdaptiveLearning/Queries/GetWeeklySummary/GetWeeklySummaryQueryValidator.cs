using FluentValidation;

namespace EduZim.Application.AdaptiveLearning.Queries.GetWeeklySummary;

public sealed class GetWeeklySummaryQueryValidator : AbstractValidator<GetWeeklySummaryQuery>
{
    public GetWeeklySummaryQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.StudentId).NotEmpty();
    }
}
