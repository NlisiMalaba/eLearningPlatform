using EduZim.Application.Identity.DTOs;
using MediatR;

namespace EduZim.Application.Identity.Commands.UpdateFontSizePreference;

public sealed record UpdateFontSizePreferenceCommand(Guid UserId, string FontSize)
    : IRequest<FontSizePreferenceDto>;
