using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using MediatR;

namespace EduZim.Application.Progress.Commands.SetDailyScreenTimeLimit;

public sealed record SetDailyScreenTimeLimitCommand(
    Guid TenantId,
    Guid StudentId,
    int? DailyScreenTimeLimitSeconds)
    : IRequest<ScreenTimeSettingsDto>, ITenantScopedRequest;
