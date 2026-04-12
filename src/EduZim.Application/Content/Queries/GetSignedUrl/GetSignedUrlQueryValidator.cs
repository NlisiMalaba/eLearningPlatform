using FluentValidation;

namespace EduZim.Application.Content.Queries.GetSignedUrl;

public sealed class GetSignedUrlQueryValidator : AbstractValidator<GetSignedUrlQuery>
{
    public GetSignedUrlQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.ContentId).NotEmpty();
        When(q => q.UrlTtl.HasValue, () =>
        {
            RuleFor(q => q.UrlTtl!.Value)
                .GreaterThan(TimeSpan.Zero)
                .LessThanOrEqualTo(TimeSpan.FromDays(7));
        });
    }
}
