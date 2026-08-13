using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.LiveClassrooms.Commands.ScheduleSession;

public sealed record ScheduleSessionCommand(
    Guid TenantId,
    Guid SchoolClassId,
    DateTime StartAtUtc,
    int DurationMinutes) : IRequest<Guid>, ITenantScopedRequest;
