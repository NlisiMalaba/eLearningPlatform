using FluentValidation;

namespace EduZim.Application.Content.Queries.GetTranscript;

public sealed class GetTranscriptQueryValidator : AbstractValidator<GetTranscriptQuery>
{
    public GetTranscriptQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.ContentId).NotEmpty();
    }
}
