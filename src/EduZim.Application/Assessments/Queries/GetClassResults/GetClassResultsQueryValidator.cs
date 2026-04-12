using FluentValidation;

namespace EduZim.Application.Assessments.Queries.GetClassResults;

public sealed class GetClassResultsQueryValidator : AbstractValidator<GetClassResultsQuery>
{
    public GetClassResultsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.AssessmentId).NotEmpty();
        RuleFor(q => q.SchoolClassId).NotEmpty();
    }
}
