using FluentValidation;

namespace EduZim.Application.Tenants.Commands.UpdateBranding;

public sealed class UpdateBrandingCommandValidator : AbstractValidator<UpdateBrandingCommand>
{
    public UpdateBrandingCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Branding.SchoolName).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Branding.PrimaryColour).MaximumLength(32);
        RuleFor(c => c.Branding.LogoUrl).MaximumLength(1024);
        RuleFor(c => c.Branding.SsoAuthorizationEndpoint).MaximumLength(2048);
    }
}
