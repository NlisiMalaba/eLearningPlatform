using FluentValidation;

namespace EduZim.Application.Progress.Queries.GetStudentProgress;

public sealed class GetStudentProgressQueryValidator : AbstractValidator<GetStudentProgressQuery>
{
    public GetStudentProgressQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.StudentId).NotEmpty();
    }
}
