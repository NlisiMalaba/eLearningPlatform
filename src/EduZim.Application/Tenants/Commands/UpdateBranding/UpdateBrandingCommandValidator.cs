using FluentValidation;

namespace EduZim.Application.Tenants.Commands.UpdateBranding;

public sealed class UpdateBrandingCommandValidator : AbstractValidator<UpdateBrandingCommand>
{
    public UpdateBrandingCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Branding.SchoolName).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Branding.PrimaryColour)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$")
            .WithMessage("Primary colour must be a hex value such as #0B6E4F.");
        RuleFor(c => c.Branding.LogoUrl).MaximumLength(1024);
        RuleFor(c => c.Branding.SsoAuthorizationEndpoint).MaximumLength(2048);
    }
}
