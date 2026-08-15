using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Content.Queries.ListContent;

public sealed class ListContentQueryHandler
    : IRequestHandler<ListContentQuery, IReadOnlyList<ContentListItemDto>>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListContentQueryHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ContentListItemDto>> Handle(ListContentQuery request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        return await _db.ContentItems.AsNoTracking()
            .Where(c => c.TenantId == request.TenantId && c.Status != ContentStatus.Archived)
            .OrderBy(c => c.Title)
            .ThenBy(c => c.Id)
            .Select(c => new ContentListItemDto(
                c.Id,
                c.Title,
                c.Type,
                c.Status,
                c.FileSizeBytes,
                c.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
