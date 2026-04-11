using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Enums;
using MediatR;

namespace EduZim.Application.Content.Commands.CreateModule;

public sealed record CreateModuleCommand(
    Guid TenantId,
    string Title,
    GradeLevel Grade,
    string Subject,
    int SequenceOrder,
    bool IsRequired) : IRequest<Guid>, ITenantScopedRequest;
