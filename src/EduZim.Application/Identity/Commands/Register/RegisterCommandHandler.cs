using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Identity.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Guid>
{
    private readonly IEmailService _emailService;
    private readonly IdentityAppSettings _identityApp;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IEmailService emailService,
        IOptions<IdentityAppSettings> identityOptions)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _identityApp = identityOptions.Value;
    }

    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existing is not null)
            throw new ConflictException("A user with this email is already registered.");

        await EnsureRoleExistsAsync(request.Role.ToString(), cancellationToken).ConfigureAwait(false);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            NormalizedUserName = _userManager.NormalizeName(request.Email),
            Email = request.Email,
            NormalizedEmail = _userManager.NormalizeEmail(request.Email),
            EmailConfirmed = false,
            TenantId = request.TenantId,
            Role = request.Role,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
        };

        var create = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!create.Succeeded)
        {
            var msg = string.Join("; ", create.Errors.Select(e => e.Description));
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Password", create.Errors.Select(e => e.Description).ToArray() },
            });
        }

        await _userManager.AddToRoleAsync(user, request.Role.ToString()).ConfigureAwait(false);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
        var baseUrl = _identityApp.PublicAppBaseUrl.TrimEnd('/');
        var path = _identityApp.EmailConfirmationRelativePath.StartsWith('/')
            ? _identityApp.EmailConfirmationRelativePath
            : "/" + _identityApp.EmailConfirmationRelativePath;
        var link =
            $"{baseUrl}{path}?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        var html =
            $"""
            <p>Please confirm your email address for EduZim.</p>
            <p><a href="{link}">Confirm email</a></p>
            """;

        await _emailService
            .SendAsync(request.Email, "Confirm your EduZim account", html, cancellationToken)
            .ConfigureAwait(false);

        return user.Id;
    }

    private async Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken)
    {
        if (await _roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            return;

        var role = new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
        };
        var r = await _roleManager.CreateAsync(role).ConfigureAwait(false);
        if (!r.Succeeded)
            throw new InvalidOperationException($"Could not create role {roleName}: {string.Join("; ", r.Errors.Select(e => e.Description))}");
    }
}
