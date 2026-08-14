using FluentValidation;

namespace EduZim.Application.Progress.Queries.GetParentDashboard;

public sealed class GetParentDashboardQueryValidator : AbstractValidator<GetParentDashboardQuery>
{
    public GetParentDashboardQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.ParentId).NotEmpty();
    }
}
