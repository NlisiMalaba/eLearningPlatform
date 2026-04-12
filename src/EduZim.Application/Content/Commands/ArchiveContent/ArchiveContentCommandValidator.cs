using FluentValidation;

namespace EduZim.Application.Content.Commands.ArchiveContent;

public sealed class ArchiveContentCommandValidator : AbstractValidator<ArchiveContentCommand>
{
    public ArchiveContentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ContentId).NotEmpty();
    }
}
