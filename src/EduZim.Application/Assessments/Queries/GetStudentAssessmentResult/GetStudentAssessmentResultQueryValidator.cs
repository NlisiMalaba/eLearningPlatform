using FluentValidation;

namespace EduZim.Application.Assessments.Queries.GetStudentAssessmentResult;

public sealed class GetStudentAssessmentResultQueryValidator : AbstractValidator<GetStudentAssessmentResultQuery>
{
    public GetStudentAssessmentResultQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.AssessmentId).NotEmpty();
        RuleFor(q => q.StudentId).NotEmpty();
    }
}
