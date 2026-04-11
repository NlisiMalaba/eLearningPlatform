using FluentValidation;

namespace EduZim.Application.Tenants.Commands.SuspendTenant;

public sealed class SuspendTenantCommandValidator : AbstractValidator<SuspendTenantCommand>
{
    public SuspendTenantCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
    }
}
