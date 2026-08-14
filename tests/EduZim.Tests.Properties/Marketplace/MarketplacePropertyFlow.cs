using EduZim.Application.Marketplace.Commands.ApproveAccess;
using EduZim.Application.Marketplace.Commands.ApproveContentPack;
using EduZim.Application.Marketplace.Commands.RequestAccess;
using EduZim.Application.Marketplace.Commands.SubmitContentPack;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Queries.BrowseContentPacks;
using EduZim.Application.Marketplace.Queries.GetContentPack;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Marketplace;

internal static class MarketplacePropertyFlow
{
    public static (EduZimDbContext Db, IMediator Mediator, MutableCurrentUser Current) Resolve(
        ServiceProvider provider,
        IServiceScope scope)
    {
        return (
            scope.ServiceProvider.GetRequiredService<EduZimDbContext>(),
            scope.ServiceProvider.GetRequiredService<IMediator>(),
            provider.GetRequiredService<MutableCurrentUser>());
    }

    public static void AsTeacher(MutableCurrentUser current, Guid tenantId, Guid userId)
    {
        current.UserId = userId;
        current.TenantId = tenantId;
        current.Role = UserRole.Teacher;
    }

    public static async Task<List<Guid>> SubmitPacksAsync(
        IMediator mediator,
        MutableCurrentUser current,
        MarketplaceSchools schools,
        int count)
    {
        AsTeacher(current, schools.OriginTenantId, schools.OriginTeacherId);
        List<Guid> ids = [];
        for (int i = 0; i < count; i++)
        {
            ContentPackDto dto = await mediator.Send(
                    new SubmitContentPackCommand(
                        schools.OriginTenantId,
                        $"Pack {i}",
                        "Shared lessons",
                        [schools.OriginContentItemId]),
                    CancellationToken.None)
                .ConfigureAwait(false);
            ids.Add(dto.Id);
        }

        return ids;
    }

    public static async Task AssertPendingAndHiddenAsync(
        EduZimDbContext db,
        IMediator mediator,
        MutableCurrentUser current,
        MarketplaceSchools schools,
        List<Guid> packIds)
    {
        List<ContentPack> stored = await db.ContentPacks.AsNoTracking().ToListAsync().ConfigureAwait(false);
        Assert.Equal(packIds.Count, stored.Count);
        Assert.All(stored, p => Assert.Equal(ContentPackStatus.PendingReview, p.Status));

        AsTeacher(current, schools.RequestingTenantId, schools.RequestingTeacherId);
        IReadOnlyList<ContentPackDto> browse = await mediator
            .Send(new BrowseContentPacksQuery(schools.RequestingTenantId), CancellationToken.None)
            .ConfigureAwait(false);
        Assert.Empty(browse);
    }

    public static async Task ApprovePacksAsync(
        IMediator mediator,
        MutableCurrentUser current,
        List<Guid> packIds)
    {
        current.Role = UserRole.PlatformAdmin;
        current.TenantId = null;
        current.UserId = Guid.NewGuid();
        foreach (Guid packId in packIds)
        {
            await mediator.Send(new ApproveContentPackCommand(packId), CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    public static async Task AssertBrowseIsExactlyApprovedAsync(
        EduZimDbContext db,
        IMediator mediator,
        MutableCurrentUser current,
        MarketplaceSchools schools,
        List<Guid> packIds,
        int approveCount)
    {
        List<ContentPack> stored = await db.ContentPacks.AsNoTracking().ToListAsync().ConfigureAwait(false);
        Assert.Equal(approveCount, stored.Count(p => p.Status == ContentPackStatus.Approved));
        Assert.Equal(packIds.Count - approveCount, stored.Count(p => p.Status == ContentPackStatus.PendingReview));

        AsTeacher(current, schools.RequestingTenantId, schools.RequestingTeacherId);
        IReadOnlyList<ContentPackDto> browse = await mediator
            .Send(new BrowseContentPacksQuery(schools.RequestingTenantId), CancellationToken.None)
            .ConfigureAwait(false);
        Assert.Equal(approveCount, browse.Count);
        Assert.All(browse, dto => Assert.Equal(ContentPackStatus.Approved, dto.Status));
        Assert.Equal(packIds.Take(approveCount).OrderBy(id => id), browse.Select(d => d.Id).OrderBy(id => id));
    }

    public static async Task<Guid> SubmitAndApproveAsync(
        IMediator mediator,
        MutableCurrentUser current,
        MarketplaceSchools schools,
        string title)
    {
        AsTeacher(current, schools.OriginTenantId, schools.OriginTeacherId);
        ContentPackDto submitted = await mediator.Send(
                new SubmitContentPackCommand(
                    schools.OriginTenantId,
                    title,
                    "Shared lessons",
                    [schools.OriginContentItemId]),
                CancellationToken.None)
            .ConfigureAwait(false);
        await ApprovePacksAsync(mediator, current, [submitted.Id]).ConfigureAwait(false);
        return submitted.Id;
    }

    public static async Task<List<Guid>> SeedOtherApprovedPacksAsync(
        EduZimDbContext db,
        MarketplaceSchools schools,
        int extraCount)
    {
        List<Guid> ids = [];
        DateTime utcNow = DateTime.UtcNow;
        for (int i = 0; i < extraCount; i++)
        {
            Guid itemId = await MarketplacePropertySeeds
                .SeedContentItemAsync(db, schools.OriginTenantId, schools.OriginTeacherId, utcNow)
                .ConfigureAwait(false);
            Guid packId = await MarketplacePropertySeeds
                .SeedApprovedPackAsync(
                    db,
                    schools.OriginTenantId,
                    schools.OriginTeacherId,
                    itemId,
                    $"Other pack {i}",
                    "Harare High",
                    "Tinashe Ncube")
                .ConfigureAwait(false);
            ids.Add(packId);
        }

        return ids;
    }

    public static async Task GrantAccessAsync(
        IMediator mediator,
        MutableCurrentUser current,
        MarketplaceSchools schools,
        Guid packId)
    {
        AsTeacher(current, schools.RequestingTenantId, schools.RequestingTeacherId);
        await mediator.Send(new RequestAccessCommand(schools.RequestingTenantId, packId), CancellationToken.None)
            .ConfigureAwait(false);
        AsTeacher(current, schools.OriginTenantId, schools.OriginTeacherId);
        await mediator.Send(
                new ApproveAccessCommand(schools.OriginTenantId, packId, schools.RequestingTenantId),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    public static async Task AssertOnlyGrantedPackAccessibleAsync(
        IMediator mediator,
        MutableCurrentUser current,
        MarketplaceSchools schools,
        Guid grantedPackId,
        List<Guid> otherPackIds)
    {
        AsTeacher(current, schools.RequestingTenantId, schools.RequestingTeacherId);
        ContentPackDetailDto granted = await mediator.Send(
                new GetContentPackQuery(schools.RequestingTenantId, grantedPackId),
                CancellationToken.None)
            .ConfigureAwait(false);
        Assert.Equal(schools.OriginContentItemId, Assert.Single(granted.ContentItemIds));
        Assert.True(granted.Pack.HasAccess);

        foreach (Guid otherId in otherPackIds)
        {
            await Assert.ThrowsAsync<TenantAccessViolationException>(
                () => mediator.Send(
                    new GetContentPackQuery(schools.RequestingTenantId, otherId),
                    CancellationToken.None));
        }
    }
}
