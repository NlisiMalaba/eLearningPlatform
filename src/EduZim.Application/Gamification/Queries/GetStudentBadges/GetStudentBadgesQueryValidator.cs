using FluentValidation;

namespace EduZim.Application.Gamification.Queries.GetStudentBadges;

public sealed class GetStudentBadgesQueryValidator : AbstractValidator<GetStudentBadgesQuery>
{
    public GetStudentBadgesQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.StudentId).NotEmpty();
    }
}
