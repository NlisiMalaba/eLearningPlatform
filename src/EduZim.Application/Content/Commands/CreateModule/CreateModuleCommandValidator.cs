using FluentValidation;

namespace EduZim.Application.Content.Commands.CreateModule;

public sealed class CreateModuleCommandValidator : AbstractValidator<CreateModuleCommand>
{
    public CreateModuleCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(128);
    }
}
