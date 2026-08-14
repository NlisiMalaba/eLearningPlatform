using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ModuleEntity = EduZim.Domain.Entities.Module;

namespace EduZim.Application.Content.Queries.GetModuleById;

public sealed class GetModuleByIdQueryHandler : IRequestHandler<GetModuleByIdQuery, ModuleDetailDto>
{
    private readonly IEduZimDbContext _db;

    public GetModuleByIdQueryHandler(IEduZimDbContext db)
    {
        _db = db;
    }

    public async Task<ModuleDetailDto> Handle(GetModuleByIdQuery request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        ModuleEntity module = await LoadModuleAsync(request, ct).ConfigureAwait(false);
        IReadOnlyList<ModuleContentItemDetailDto> items = await LoadItemsAsync(request, ct).ConfigureAwait(false);
        return Map(module, items);
    }

    private async Task<ModuleEntity> LoadModuleAsync(GetModuleByIdQuery request, CancellationToken ct)
    {
        ModuleEntity? module = await _db.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.ModuleId && m.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (module is null)
            throw new NotFoundException(nameof(ModuleEntity), request.ModuleId);

        return module;
    }

    private async Task<IReadOnlyList<ModuleContentItemDetailDto>> LoadItemsAsync(
        GetModuleByIdQuery request,
        CancellationToken ct)
    {
        List<ModuleContentItem> links = await _db.ModuleContentItems.AsNoTracking()
            .Where(l => l.ModuleId == request.ModuleId && l.TenantId == request.TenantId)
            .OrderBy(l => l.SequenceOrder)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<Guid> contentIds = links.Select(l => l.ContentItemId).ToList();
        Dictionary<Guid, ContentItemMeta> meta = await _db.ContentItems.AsNoTracking()
            .Where(c => contentIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new ContentItemMeta(c.Title, c.Type), ct)
            .ConfigureAwait(false);

        return links.Select(l => MapItem(l, meta)).ToList();
    }

    private static ModuleContentItemDetailDto MapItem(
        ModuleContentItem link,
        IReadOnlyDictionary<Guid, ContentItemMeta> meta)
    {
        ContentItemMeta? item = meta.GetValueOrDefault(link.ContentItemId);
        return new ModuleContentItemDetailDto
        {
            ContentItemId = link.ContentItemId,
            SequenceOrder = link.SequenceOrder,
            Title = item?.Title ?? string.Empty,
            Type = item?.Type ?? default,
        };
    }

    private static ModuleDetailDto Map(ModuleEntity module, IReadOnlyList<ModuleContentItemDetailDto> items)
    {
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

    private sealed record ContentItemMeta(string Title, ContentType Type);
}
