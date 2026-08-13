using FluentValidation;

namespace EduZim.Application.Gamification.Queries.GetStudentPoints;

public sealed class GetStudentPointsQueryValidator : AbstractValidator<GetStudentPointsQuery>
{
    public GetStudentPointsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.StudentId).NotEmpty();
    }
}
