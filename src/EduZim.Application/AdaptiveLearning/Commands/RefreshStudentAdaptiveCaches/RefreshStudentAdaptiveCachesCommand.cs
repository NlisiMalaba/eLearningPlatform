using EduZim.Application.Common.Interfaces;
using MediatR;

namespace EduZim.Application.AdaptiveLearning.Commands.RefreshStudentAdaptiveCaches;

public sealed record RefreshStudentAdaptiveCachesCommand(Guid TenantId, Guid StudentId)
    : IRequest<Unit>, ITenantScopedRequest;
