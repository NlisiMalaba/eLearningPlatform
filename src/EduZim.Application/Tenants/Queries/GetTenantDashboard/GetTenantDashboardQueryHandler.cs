using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants.Models;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Tenants.Queries.GetTenantDashboard;

public sealed class GetTenantDashboardQueryHandler : IRequestHandler<GetTenantDashboardQuery, TenantDashboardDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetTenantDashboardQueryHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TenantDashboardDto> Handle(GetTenantDashboardQuery request, CancellationToken cancellationToken)
    {
        TenantAccessHelper.EnsureCanViewTenantDashboard(_currentUser, request.TenantId);

        var exists = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(Tenant), request.TenantId);

        var enrolledStudents = await _db.Users.AsNoTracking()
            .CountAsync(
                u => u.TenantId == request.TenantId && u.Role == UserRole.Student,
                cancellationToken)
            .ConfigureAwait(false);

        var activeTeachers = await _db.Users.AsNoTracking()
            .CountAsync(
                u => u.TenantId == request.TenantId
                     && u.Role == UserRole.Teacher
                     && (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);

        var subscription = await _db.Subscriptions.AsNoTracking()
            .Where(s => s.TenantId == request.TenantId)
            .OrderByDescending(s => s.CurrentPeriodEnd)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var storageBytes = await _db.ContentItems.AsNoTracking()
            .Where(c => c.TenantId == request.TenantId)
            .SumAsync(c => c.FileSizeBytes, cancellationToken)
            .ConfigureAwait(false);

        return new TenantDashboardDto(
            enrolledStudents,
            activeTeachers,
            subscription?.Status,
            storageBytes);
    }
}
