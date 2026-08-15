using FluentValidation;

namespace EduZim.Application.ZimBot.Commands.Chat;

public sealed class ChatCommandValidator : AbstractValidator<ChatCommand>
{
    public ChatCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.StudentId).NotEmpty();
        RuleFor(c => c.Message).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.Language).MaximumLength(32);
    }
}
