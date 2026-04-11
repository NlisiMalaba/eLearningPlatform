using System.Security.Cryptography;
using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduZim.Application.Tenants.Commands.GenerateInviteCode;

public sealed class GenerateInviteCodeCommandHandler : IRequestHandler<GenerateInviteCodeCommand, GeneratedInviteCodeDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TenantLifecycleSettings _lifecycle;

    public GenerateInviteCodeCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IOptions<TenantLifecycleSettings> lifecycle)
    {
        _db = db;
        _currentUser = currentUser;
        _lifecycle = lifecycle.Value;
    }

    public async Task<GeneratedInviteCodeDto> Handle(GenerateInviteCodeCommand request, CancellationToken cancellationToken)
    {
        TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, request.TenantId);

        if (_currentUser.Role is not (UserRole.SchoolAdmin or UserRole.PlatformAdmin))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Role", new[] { "Only a school administrator or platform administrator can generate invite codes." } },
            });

        var tenantExists = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!tenantExists)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        var student = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Id == request.StudentUserId && u.TenantId == request.TenantId,
                cancellationToken)
            .ConfigureAwait(false);
        if (student is null || student.Role != UserRole.Student)
            throw new NotFoundException(nameof(ApplicationUser), request.StudentUserId);

        var code = await GenerateUniqueCodeAsync(cancellationToken).ConfigureAwait(false);
        var expires = DateTime.UtcNow.AddDays(_lifecycle.InviteCodeLifetimeDays);

        var entity = new TenantInviteCode
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            StudentUserId = request.StudentUserId,
            Code = code,
            ExpiresAtUtc = expires,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _db.TenantInviteCodes.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new GeneratedInviteCodeDto(code, expires);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 32;
        const int codeBytes = 9;
        for (var i = 0; i < maxAttempts; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(codeBytes);
            var code = Convert.ToHexString(bytes)[..12];
            var exists = await _db.TenantInviteCodes.AsNoTracking()
                .AnyAsync(c => c.Code == code, cancellationToken)
                .ConfigureAwait(false);
            if (!exists)
                return code;
        }

        throw new InvalidOperationException("Could not generate a unique invite code.");
    }
}
