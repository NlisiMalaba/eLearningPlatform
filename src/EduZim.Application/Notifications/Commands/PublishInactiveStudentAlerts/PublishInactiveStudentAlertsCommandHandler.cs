using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Notifications.Commands.PublishInactiveStudentAlerts;

public sealed class PublishInactiveStudentAlertsCommandHandler
    : IRequestHandler<PublishInactiveStudentAlertsCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly IPublisher _publisher;
    private readonly ILogger<PublishInactiveStudentAlertsCommandHandler> _logger;

    public PublishInactiveStudentAlertsCommandHandler(
        IEduZimDbContext db,
        IPublisher publisher,
        ILogger<PublishInactiveStudentAlertsCommandHandler> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Unit> Handle(PublishInactiveStudentAlertsCommand request, CancellationToken ct)
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime cutoff = InactivityAlertRules.CutoffUtc(utcNow);
        List<Guid> tenantIds = await LoadActiveTenantIdsAsync(ct).ConfigureAwait(false);

        foreach (Guid tenantId in tenantIds)
            await PublishForTenantAsync(tenantId, cutoff, ct).ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task<List<Guid>> LoadActiveTenantIdsAsync(CancellationToken ct)
    {
        return await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task PublishForTenantAsync(Guid tenantId, DateTime cutoff, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(tenantId, ct).ConfigureAwait(false);

        List<InactiveStudent> students = await LoadInactiveStudentsAsync(tenantId, cutoff, ct)
            .ConfigureAwait(false);
        foreach (InactiveStudent student in students)
        {
            await _publisher
                .Publish(
                    new StudentInactiveNotification(student.Id, tenantId, student.LastLoginAt),
                    ct)
                .ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Published inactivity alerts for {Count} student(s) in tenant {TenantId}.",
            students.Count,
            tenantId);
    }

    private async Task<List<InactiveStudent>> LoadInactiveStudentsAsync(
        Guid tenantId,
        DateTime cutoff,
        CancellationToken ct)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(
                u => u.TenantId == tenantId
                    && u.Role == UserRole.Student
                    && u.LastLoginAt != null
                    && u.LastLoginAt <= cutoff
                    && (u.LastInactivityAlertAt == null || u.LastInactivityAlertAt < u.LastLoginAt))
            .Select(u => new InactiveStudent(u.Id, u.LastLoginAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private sealed record InactiveStudent(Guid Id, DateTime? LastLoginAt);
}
