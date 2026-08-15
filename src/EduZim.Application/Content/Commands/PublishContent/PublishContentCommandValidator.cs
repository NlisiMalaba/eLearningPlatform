using FluentValidation;

namespace EduZim.Application.Content.Commands.PublishContent;

public sealed class PublishContentCommandValidator : AbstractValidator<PublishContentCommand>
{
    public PublishContentCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ContentId).NotEmpty();
    }
}
