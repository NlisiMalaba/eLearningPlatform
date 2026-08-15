using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Identity.DTOs;
using EduZim.Application.Identity.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Identity.Commands.UpdateFontSizePreference;

public sealed class UpdateFontSizePreferenceCommandHandler
    : IRequestHandler<UpdateFontSizePreferenceCommand, FontSizePreferenceDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateFontSizePreferenceCommandHandler> _logger;

    public UpdateFontSizePreferenceCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<UpdateFontSizePreferenceCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<FontSizePreferenceDto> Handle(
        UpdateFontSizePreferenceCommand request,
        CancellationToken ct)
    {
        UserPreferenceAccess.EnsureCanUpdateFontSize(_currentUser, request.UserId);
        if (!FontSizePreferenceRules.TryParse(request.FontSize, out FontSize fontSize))
            throw new DomainException("Font size must be Small, Medium, Large, or ExtraLarge.");

        ApplicationUser user = await LoadUserAsync(request.UserId, ct).ConfigureAwait(false);
        EnsureTenantMatch(user);
        user.FontSize = fontSize;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Updated font size preference for user {UserId}.", request.UserId);
        return new FontSizePreferenceDto(user.Id, fontSize.ToString());
    }

    private async Task<ApplicationUser> LoadUserAsync(Guid userId, CancellationToken ct)
    {
        if (_currentUser.TenantId is { } tenantId)
            await _db.SetSessionTenantIdAsync(tenantId, ct).ConfigureAwait(false);

        ApplicationUser? user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false);
        if (user is null)
            throw new NotFoundException(nameof(ApplicationUser), userId);

        return user;
    }

    private void EnsureTenantMatch(ApplicationUser user)
    {
        if (_currentUser.Role == UserRole.PlatformAdmin)
            return;

        if (user.TenantId != _currentUser.TenantId)
            throw new TenantAccessViolationException(
                "You do not have access to this user.",
                _currentUser.TenantId,
                user.Id);
    }
}
