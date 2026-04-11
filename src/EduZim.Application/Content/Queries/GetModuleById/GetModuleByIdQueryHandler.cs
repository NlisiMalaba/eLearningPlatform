using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Content.Queries.GetModuleById;

public sealed class GetModuleByIdQueryHandler : IRequestHandler<GetModuleByIdQuery, ModuleDetailDto>
{
    private readonly IEduZimDbContext _db;

    public GetModuleByIdQueryHandler(IEduZimDbContext db)
    {
        _db = db;
    }

    public async Task<ModuleDetailDto> Handle(GetModuleByIdQuery request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var module = await _db.Modules.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == request.ModuleId && m.TenantId == request.TenantId,
                cancellationToken)
            .ConfigureAwait(false);
        if (module is null)
            throw new NotFoundException(nameof(EduZim.Domain.Entities.Module), request.ModuleId);

        var links = await _db.ModuleContentItems.AsNoTracking()
            .Where(l => l.ModuleId == request.ModuleId && l.TenantId == request.TenantId)
            .OrderBy(l => l.SequenceOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var contentIds = links.Select(l => l.ContentItemId).ToList();
        var titles = await _db.ContentItems.AsNoTracking()
            .Where(c => contentIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Title, cancellationToken)
            .ConfigureAwait(false);

        var items = links
            .Select(l => new ModuleContentItemDetailDto
            {
                ContentItemId = l.ContentItemId,
                SequenceOrder = l.SequenceOrder,
                Title = titles.GetValueOrDefault(l.ContentItemId) ?? string.Empty,
            })
            .ToList();

        return new ModuleDetailDto
        {
            Id = module.Id,
            Title = module.Title,
            Grade = module.Grade,
            Subject = module.Subject,
            SequenceOrder = module.SequenceOrder,
            IsRequired = module.IsRequired,
            ContentItems = items,
        };
    }
}
