using FluentValidation;

namespace EduZim.Application.Tenants.Commands.RestoreTenant;

public sealed class RestoreTenantCommandValidator : AbstractValidator<RestoreTenantCommand>
{
    public RestoreTenantCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
    }
}
