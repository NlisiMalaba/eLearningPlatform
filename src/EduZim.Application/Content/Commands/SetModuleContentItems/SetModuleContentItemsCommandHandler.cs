using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Queries.GetModuleById;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Content.Commands.SetModuleContentItems;

public sealed class SetModuleContentItemsCommandHandler
    : IRequestHandler<SetModuleContentItemsCommand, ModuleDetailDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IMediator _mediator;
    private readonly ILogger<SetModuleContentItemsCommandHandler> _logger;

    public SetModuleContentItemsCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IMediator mediator,
        ILogger<SetModuleContentItemsCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ModuleDetailDto> Handle(SetModuleContentItemsCommand request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        await EnsureModuleExistsAsync(request, ct).ConfigureAwait(false);
        await EnsureContentExistsAsync(request, ct).ConfigureAwait(false);
        await ReplaceLinksAsync(request, ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Updated content sequence for module {ModuleId} ({Count} item(s)).",
            request.ModuleId,
            request.ContentItemIds.Count);
        return await _mediator.Send(new GetModuleByIdQuery(request.TenantId, request.ModuleId), ct)
            .ConfigureAwait(false);
    }

    private async Task EnsureModuleExistsAsync(SetModuleContentItemsCommand request, CancellationToken ct)
    {
        bool exists = await _db.Modules.AsNoTracking()
            .AnyAsync(m => m.Id == request.ModuleId && m.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(Module), request.ModuleId);
    }

    private async Task EnsureContentExistsAsync(SetModuleContentItemsCommand request, CancellationToken ct)
    {
        if (request.ContentItemIds.Count == 0)
            return;

        int found = await _db.ContentItems.AsNoTracking()
            .CountAsync(
                c => request.ContentItemIds.Contains(c.Id)
                    && c.TenantId == request.TenantId
                    && c.Status != ContentStatus.Archived,
                ct)
            .ConfigureAwait(false);
        if (found != request.ContentItemIds.Count)
            throw new NotFoundException(nameof(ContentItem), request.ModuleId);
    }

    private async Task ReplaceLinksAsync(SetModuleContentItemsCommand request, CancellationToken ct)
    {
        List<ModuleContentItem> existing = await _db.ModuleContentItems
            .Where(l => l.ModuleId == request.ModuleId && l.TenantId == request.TenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        _db.ModuleContentItems.RemoveRange(existing);

        DateTime utcNow = DateTime.UtcNow;
        for (int i = 0; i < request.ContentItemIds.Count; i++)
        {
            await _db.ModuleContentItems.AddAsync(
                    new ModuleContentItem
                    {
                        Id = Guid.NewGuid(),
                        TenantId = request.TenantId,
                        ModuleId = request.ModuleId,
                        ContentItemId = request.ContentItemIds[i],
                        SequenceOrder = i + 1,
                        CreatedAt = utcNow,
                        UpdatedAt = utcNow,
                    },
                    ct)
                .ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
