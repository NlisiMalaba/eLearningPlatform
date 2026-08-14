using EduZim.Application.Common.Interfaces;
using EduZim.Application.ZimBot.DTOs;
using MediatR;

namespace EduZim.Application.ZimBot.Commands.Chat;

public sealed record ChatCommand(
    Guid TenantId,
    Guid StudentId,
    string Message,
    Guid? ModuleId,
    bool InAssessment) : IRequest<ZimBotChatDto>, ITenantScopedRequest;
