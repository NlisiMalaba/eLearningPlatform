using EduZim.Application.Common.Interfaces;
using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Services;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Gamification.Queries.GetStudentPoints;

public sealed class GetStudentPointsQueryHandler : IRequestHandler<GetStudentPointsQuery, StudentPointsDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetStudentPointsQueryHandler> _logger;

    public GetStudentPointsQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetStudentPointsQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<StudentPointsDto> Handle(GetStudentPointsQuery request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        await GamificationStudentAccess
            .EnsureCanViewAsync(_db, _currentUser, request.TenantId, request.StudentId, ct)
            .ConfigureAwait(false);

        StudentPoints? record = await _db.StudentPoints
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.TenantId == request.TenantId && p.StudentId == request.StudentId,
                ct)
            .ConfigureAwait(false);

        int total = record?.TotalPoints ?? 0;
        _logger.LogDebug("Student points queried for student {StudentId}.", request.StudentId);
        return new StudentPointsDto(request.StudentId, total);
    }
}
