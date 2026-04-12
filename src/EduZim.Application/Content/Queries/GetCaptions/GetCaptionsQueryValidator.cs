using FluentValidation;

namespace EduZim.Application.Content.Queries.GetCaptions;

public sealed class GetCaptionsQueryValidator : AbstractValidator<GetCaptionsQuery>
{
    public GetCaptionsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.ContentId).NotEmpty();
    }
}
