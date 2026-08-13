using FluentValidation;

namespace EduZim.Application.Notifications.Queries.GetInAppNotifications;

public sealed class GetInAppNotificationsQueryValidator : AbstractValidator<GetInAppNotificationsQuery>
{
    public GetInAppNotificationsQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.UserId).NotEmpty();
    }
}
