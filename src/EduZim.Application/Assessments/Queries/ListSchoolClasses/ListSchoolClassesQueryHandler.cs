using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Assessments.Queries.ListSchoolClasses;

public sealed class ListSchoolClassesQueryHandler
    : IRequestHandler<ListSchoolClassesQuery, IReadOnlyList<SchoolClassListItemDto>>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListSchoolClassesQueryHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SchoolClassListItemDto>> Handle(
        ListSchoolClassesQuery request,
        CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        return await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.TenantId == request.TenantId)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new SchoolClassListItemDto(c.Id, c.Name))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
