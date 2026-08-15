using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Content.Queries.ListModules;

public sealed class ListModulesQueryHandler
    : IRequestHandler<ListModulesQuery, IReadOnlyList<ModuleListItemDto>>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListModulesQueryHandler(IEduZimDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ModuleListItemDto>> Handle(ListModulesQuery request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        List<Module> modules = await _db.Modules.AsNoTracking()
            .Where(m => m.TenantId == request.TenantId)
            .OrderBy(m => m.Grade)
            .ThenBy(m => m.SequenceOrder)
            .ThenBy(m => m.Title)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<ModuleContentItem> links = await _db.ModuleContentItems.AsNoTracking()
            .Where(l => l.TenantId == request.TenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        Dictionary<Guid, int> counts = links
            .GroupBy(l => l.ModuleId)
            .ToDictionary(g => g.Key, g => g.Count());

        return modules
            .Select(m => new ModuleListItemDto(
                m.Id,
                m.Title,
                m.Grade,
                m.Subject,
                m.SequenceOrder,
                m.IsRequired,
                counts.GetValueOrDefault(m.Id)))
            .ToList();
    }
}
