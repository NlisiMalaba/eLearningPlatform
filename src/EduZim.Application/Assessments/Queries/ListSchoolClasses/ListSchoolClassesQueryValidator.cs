using FluentValidation;

namespace EduZim.Application.Assessments.Queries.ListSchoolClasses;

public sealed class ListSchoolClassesQueryValidator : AbstractValidator<ListSchoolClassesQuery>
{
    public ListSchoolClassesQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
