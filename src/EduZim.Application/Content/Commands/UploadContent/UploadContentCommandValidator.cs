using FluentValidation;

namespace EduZim.Application.Content.Commands.UploadContent;

public sealed class UploadContentCommandValidator : AbstractValidator<UploadContentCommand>
{
    public UploadContentCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Language).NotEmpty().MaximumLength(32);
        RuleFor(x => x.ContentTypeHeader).NotEmpty().MaximumLength(256);
        RuleFor(x => x.FileSizeBytes).GreaterThan(0);
    }
}
