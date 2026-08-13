using FluentValidation;

namespace EduZim.Application.LiveClassrooms.Queries.GetRecordingUrl;

public sealed class GetRecordingUrlQueryValidator : AbstractValidator<GetRecordingUrlQuery>
{
    public GetRecordingUrlQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.SessionId).NotEmpty();
    }
}
