using FluentValidation;

namespace EduZim.Application.Gamification.Queries.GetLeaderboard;

public sealed class GetLeaderboardQueryValidator : AbstractValidator<GetLeaderboardQuery>
{
    public GetLeaderboardQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.Limit).InclusiveBetween(1, 100);
    }
}
