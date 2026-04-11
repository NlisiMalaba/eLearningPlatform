using FluentValidation;

namespace EduZim.Application.Tenants.Commands.ProvisionTenant;

public sealed class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(256);
    }
}
