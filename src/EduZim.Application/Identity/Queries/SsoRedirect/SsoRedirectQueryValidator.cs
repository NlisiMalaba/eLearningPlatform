using FluentValidation;

namespace EduZim.Application.Identity.Queries.SsoRedirect;

public sealed class SsoRedirectQueryValidator : AbstractValidator<SsoRedirectQuery>
{
    public SsoRedirectQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
