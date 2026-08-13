using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.LiveClassrooms.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.LiveClassrooms.Commands.ScheduleSession;

public sealed class ScheduleSessionCommandHandler : IRequestHandler<ScheduleSessionCommand, Guid>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPublisher _publisher;
    private readonly ILogger<ScheduleSessionCommandHandler> _logger;

    public ScheduleSessionCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IPublisher publisher,
        ILogger<ScheduleSessionCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Guid> Handle(ScheduleSessionCommand request, CancellationToken ct)
    {
        LiveClassroomAccess.EnsureCanSchedule(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        await EnsureClassExistsAsync(request, ct).ConfigureAwait(false);

        DateTime utcNow = DateTime.UtcNow;
        Guid sessionId = Guid.NewGuid();
        ClassroomSession session = new ClassroomSession
        {
            Id = sessionId,
            TenantId = request.TenantId,
            SchoolClassId = request.SchoolClassId,
            TeacherUserId = _currentUser.UserId,
            StartAtUtc = request.StartAtUtc,
            PlannedEndAtUtc = request.StartAtUtc.AddMinutes(request.DurationMinutes),
            RoomId = sessionId.ToString("N"),
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        await _db.ClassroomSessions.AddAsync(session, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _publisher
            .Publish(new ClassroomScheduledNotification(sessionId, request.TenantId), ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Scheduled classroom session {SessionId} for class {ClassId} starting {StartAtUtc}.",
            sessionId,
            request.SchoolClassId,
            request.StartAtUtc);
        return sessionId;
    }

    private async Task EnsureClassExistsAsync(ScheduleSessionCommand request, CancellationToken ct)
    {
        bool exists = await _db.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.SchoolClassId && c.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(SchoolClass), request.SchoolClassId);
    }
}
