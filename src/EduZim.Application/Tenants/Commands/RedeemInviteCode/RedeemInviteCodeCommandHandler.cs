using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Tenants.Commands.RedeemInviteCode;

public sealed class RedeemInviteCodeCommandHandler : IRequestHandler<RedeemInviteCodeCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RedeemInviteCodeCommandHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RedeemInviteCodeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.ParentGuardian)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Role", new[] { "Only a parent or guardian account can redeem this invite code." } },
            });

        var invite = await _db.TenantInviteCodes
            .FirstOrDefaultAsync(c => c.Code == request.Code, cancellationToken)
            .ConfigureAwait(false);
        if (invite is null)
            throw new NotFoundException(nameof(TenantInviteCode), request.Code);

        if (invite.UsedAtUtc is not null)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Code", new[] { "This invite code has already been used." } },
            });

        if (invite.ExpiresAtUtc < DateTime.UtcNow)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Code", new[] { "This invite code has expired." } },
            });

        var parent = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (parent is null)
            throw new NotFoundException(nameof(ApplicationUser), _currentUser.UserId);

        var student = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == invite.StudentUserId, cancellationToken)
            .ConfigureAwait(false);
        if (student is null || student.TenantId != invite.TenantId || student.Role != UserRole.Student)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Code", new[] { "The student associated with this code is no longer valid." } },
            });

        var existing = await _db.ParentStudentLinks.AsNoTracking()
            .AnyAsync(
                l => l.TenantId == invite.TenantId
                     && l.ParentUserId == parent.Id
                     && l.StudentUserId == student.Id,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing)
        {
            invite.UsedAtUtc = DateTime.UtcNow;
            invite.UsedByParentUserId = parent.Id;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Unit.Value;
        }

        var link = new ParentStudentLink
        {
            Id = Guid.NewGuid(),
            TenantId = invite.TenantId,
            ParentUserId = parent.Id,
            StudentUserId = student.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _db.ParentStudentLinks.AddAsync(link, cancellationToken).ConfigureAwait(false);
        invite.UsedAtUtc = DateTime.UtcNow;
        invite.UsedByParentUserId = parent.Id;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
