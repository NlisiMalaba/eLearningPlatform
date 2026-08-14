using FluentValidation;

namespace EduZim.Application.Assessments.Queries.ListAssessments;

public sealed class ListAssessmentsQueryValidator : AbstractValidator<ListAssessmentsQuery>
{
    public ListAssessmentsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
