using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using MediatR;

namespace EduZim.Application.Progress.Commands.RecordStudentInteraction;

public sealed record RecordStudentInteractionCommand(Guid TenantId, Guid StudentId)
    : IRequest<StudentSessionDto>, ITenantScopedRequest;
