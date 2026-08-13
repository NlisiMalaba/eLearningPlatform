using FluentValidation;

namespace EduZim.Application.LiveClassrooms.Queries.GetAttendance;

public sealed class GetAttendanceQueryValidator : AbstractValidator<GetAttendanceQuery>
{
    public GetAttendanceQueryValidator()
    {
        RuleFor(q => q.TenantId).NotEmpty();
        RuleFor(q => q.SessionId).NotEmpty();
    }
}
