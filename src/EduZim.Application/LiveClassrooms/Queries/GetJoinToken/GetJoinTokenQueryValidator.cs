using FluentValidation;

namespace EduZim.Application.LiveClassrooms.Queries.GetJoinToken;

public sealed class GetJoinTokenQueryValidator : AbstractValidator<GetJoinTokenQuery>
{
    public GetJoinTokenQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.SessionId).NotEmpty();
    }
}
