using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Identity.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string? FullName,
    string? PhoneNumber,
    UserRole Role,
    Guid? TenantId) : IRequest<Guid>;
