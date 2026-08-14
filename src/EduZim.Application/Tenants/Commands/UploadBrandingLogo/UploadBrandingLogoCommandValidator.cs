using FluentValidation;

namespace EduZim.Application.Tenants.Commands.UploadBrandingLogo;

public sealed class UploadBrandingLogoCommandValidator : AbstractValidator<UploadBrandingLogoCommand>
{
    public UploadBrandingLogoCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.FileSizeBytes).GreaterThan(0);
        RuleFor(c => c.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Content).NotNull();
    }
}
