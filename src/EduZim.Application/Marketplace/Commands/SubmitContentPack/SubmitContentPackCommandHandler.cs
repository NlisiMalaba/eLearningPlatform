using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Marketplace.Commands.SubmitContentPack;

public sealed class SubmitContentPackCommandHandler : IRequestHandler<SubmitContentPackCommand, ContentPackDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SubmitContentPackCommandHandler> _logger;

    public SubmitContentPackCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<SubmitContentPackCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ContentPackDto> Handle(SubmitContentPackCommand request, CancellationToken ct)
    {
        MarketplaceAccess.EnsureCanSubmit(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        (string schoolName, string teacherName) = await LoadAttributionAsync(request.TenantId, ct)
            .ConfigureAwait(false);
        IReadOnlyList<Guid> itemIds = await LoadOwnedContentItemIdsAsync(request, ct).ConfigureAwait(false);

        DateTime utcNow = DateTime.UtcNow;
        ContentPack pack = MarketplacePackFactory.CreatePending(
            request,
            _currentUser.UserId,
            schoolName,
            teacherName,
            utcNow);
        await _db.ContentPacks.AddAsync(pack, ct).ConfigureAwait(false);
        await AddItemsAsync(pack, itemIds, utcNow, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Submitted content pack {PackId} with status PendingReview.", pack.Id);
        return MarketplaceMapper.ToDto(pack, [], hasAccess: true);
    }

    private async Task<(string SchoolName, string TeacherName)> LoadAttributionAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        Tenant tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        ApplicationUser teacher = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ApplicationUser), _currentUser.UserId);

        string schoolName = ResolveSchoolName(tenant);
        string teacherName = teacher.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(schoolName) || string.IsNullOrWhiteSpace(teacherName))
        {
            throw new DomainException(
                "Marketplace packs require a school name and teacher name for attribution.");
        }

        return (schoolName, teacherName);
    }

    private static string ResolveSchoolName(Tenant tenant)
    {
        string? branded = tenant.Branding?.SchoolName;
        if (!string.IsNullOrWhiteSpace(branded))
            return branded.Trim();
        return tenant.Name.Trim();
    }

    private async Task<IReadOnlyList<Guid>> LoadOwnedContentItemIdsAsync(
        SubmitContentPackCommand request,
        CancellationToken ct)
    {
        List<Guid> distinctIds = request.ContentItemIds.Distinct().ToList();
        List<Guid> found = await _db.ContentItems.AsNoTracking()
            .Where(
                c => c.TenantId == request.TenantId
                    && distinctIds.Contains(c.Id)
                    && c.ArchivedAt == null
                    && c.Status != ContentStatus.Archived)
            .Select(c => c.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (found.Count != distinctIds.Count)
            throw new NotFoundException(nameof(ContentItem), distinctIds.First(id => !found.Contains(id)));

        return distinctIds;
    }

    private async Task AddItemsAsync(
        ContentPack pack,
        IReadOnlyList<Guid> itemIds,
        DateTime utcNow,
        CancellationToken ct)
    {
        for (int i = 0; i < itemIds.Count; i++)
        {
            ContentPackItem item = MarketplacePackFactory.CreateItem(
                pack.Id,
                pack.TenantId,
                itemIds[i],
                i,
                utcNow);
            await _db.ContentPackItems.AddAsync(item, ct).ConfigureAwait(false);
        }
    }
}
