using FluentValidation;

namespace EduZim.Application.Marketplace.Commands.RequestAccess;

public sealed class RequestAccessCommandValidator : AbstractValidator<RequestAccessCommand>
{
    public RequestAccessCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.ContentPackId).NotEmpty();
    }
}
