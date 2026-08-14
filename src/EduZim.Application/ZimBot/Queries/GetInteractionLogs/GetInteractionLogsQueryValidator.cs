using FluentValidation;

namespace EduZim.Application.ZimBot.Queries.GetInteractionLogs;

public sealed class GetInteractionLogsQueryValidator : AbstractValidator<GetInteractionLogsQuery>
{
    public GetInteractionLogsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
    }
}
