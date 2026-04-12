using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Commands.AssignAssessment;

public sealed class AssignAssessmentCommandHandler : IRequestHandler<AssignAssessmentCommand, Unit>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPublisher _publisher;
    private readonly ILogger<AssignAssessmentCommandHandler> _logger;

    public AssignAssessmentCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IPublisher publisher,
        ILogger<AssignAssessmentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Unit> Handle(AssignAssessmentCommand request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        Assessment? assessment = await _db.Assessments
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId && a.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (assessment is null)
            throw new NotFoundException(nameof(Assessment), request.AssessmentId);

        SchoolClass? schoolClass = await _db.SchoolClasses
            .FirstOrDefaultAsync(c => c.Id == request.SchoolClassId && c.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (schoolClass is null)
            throw new NotFoundException(nameof(SchoolClass), request.SchoolClassId);

        DateTime now = DateTime.UtcNow;
        AssessmentClassAssignment? existing = await _db.AssessmentClassAssignments
            .FirstOrDefaultAsync(
                x => x.AssessmentId == request.AssessmentId && x.SchoolClassId == request.SchoolClassId,
                ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.DueAtUtc = request.DueAtUtc;
            existing.AssignedAtUtc = now;
            existing.UpdatedAt = now;
        }
        else
        {
            var row = new AssessmentClassAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                AssessmentId = request.AssessmentId,
                SchoolClassId = request.SchoolClassId,
                DueAtUtc = request.DueAtUtc,
                AssignedAtUtc = now,
                CreatedAt = now,
                UpdatedAt = now,
            };
            await _db.AssessmentClassAssignments.AddAsync(row, ct).ConfigureAwait(false);
            existing = row;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _publisher
            .Publish(new AssessmentAssignedNotification(existing!.Id, request.TenantId), ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Assigned assessment {AssessmentId} to class {ClassId} in tenant {TenantId}.",
            request.AssessmentId,
            request.SchoolClassId,
            request.TenantId);

        return Unit.Value;
    }
}
