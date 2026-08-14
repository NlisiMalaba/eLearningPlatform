using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Queries.BrowseContentPacks;
using EduZim.Application.Marketplace.Queries.GetContentPack;
using EduZim.Domain.Exceptions;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck.Xunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Marketplace;

/// <summary>Feature: elearning-app-zimbabwe — Marketplace properties 21, 22, 23.</summary>
public sealed class MarketplacePropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 21: Marketplace Submission Requires Admin Review — Validates: Requirements 8.2
    [Property(MaxTest = 100)]
    public async Task Property21_submitted_pack_not_discoverable_until_approved(
        byte pendingRaw,
        byte approveRaw)
    {
        int pendingCount = (pendingRaw % 4) + 1;
        int approveCount = approveRaw % (pendingCount + 1);
        using ServiceProvider provider = MarketplacePropertyTestHost.Create();
        using IServiceScope scope = MarketplacePropertyTestHost.CreateScope(provider);
        (EduZimDbContext db, IMediator mediator, MutableCurrentUser current) =
            MarketplacePropertyFlow.Resolve(provider, scope);
        MarketplaceSchools schools = await MarketplacePropertySeeds
            .SeedTwoSchoolsAsync(db, "Mufakose Primary", "Agnes Moyo")
            .ConfigureAwait(false);

        List<Guid> packIds = await MarketplacePropertyFlow
            .SubmitPacksAsync(mediator, current, schools, pendingCount)
            .ConfigureAwait(false);
        await MarketplacePropertyFlow
            .AssertPendingAndHiddenAsync(db, mediator, current, schools, packIds)
            .ConfigureAwait(false);
        await MarketplacePropertyFlow
            .ApprovePacksAsync(mediator, current, packIds.Take(approveCount).ToList())
            .ConfigureAwait(false);
        await MarketplacePropertyFlow
            .AssertBrowseIsExactlyApprovedAsync(db, mediator, current, schools, packIds, approveCount)
            .ConfigureAwait(false);
    }

    // Feature: elearning-app-zimbabwe, Property 22: Cross-Tenant Content Access Requires Approval — Validates: Requirements 8.3, 8.4
    [Property(MaxTest = 100)]
    public async Task Property22_content_access_requires_originating_teacher_approval(byte extraPacksRaw)
    {
        int extraCount = (extraPacksRaw % 3) + 1;
        using ServiceProvider provider = MarketplacePropertyTestHost.Create();
        using IServiceScope scope = MarketplacePropertyTestHost.CreateScope(provider);
        (EduZimDbContext db, IMediator mediator, MutableCurrentUser current) =
            MarketplacePropertyFlow.Resolve(provider, scope);
        MarketplaceSchools schools = await MarketplacePropertySeeds
            .SeedTwoSchoolsAsync(db, "Harare High", "Tinashe Ncube")
            .ConfigureAwait(false);

        Guid grantedPackId = await MarketplacePropertyFlow
            .SubmitAndApproveAsync(mediator, current, schools, "Granted pack")
            .ConfigureAwait(false);
        List<Guid> otherPackIds = await MarketplacePropertyFlow
            .SeedOtherApprovedPacksAsync(db, schools, extraCount)
            .ConfigureAwait(false);

        MarketplacePropertyFlow.AsTeacher(current, schools.RequestingTenantId, schools.RequestingTeacherId);
        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => mediator.Send(
                new GetContentPackQuery(schools.RequestingTenantId, grantedPackId),
                CancellationToken.None));
        await MarketplacePropertyFlow.GrantAccessAsync(mediator, current, schools, grantedPackId)
            .ConfigureAwait(false);
        await MarketplacePropertyFlow
            .AssertOnlyGrantedPackAccessibleAsync(mediator, current, schools, grantedPackId, otherPackIds)
            .ConfigureAwait(false);
    }

    // Feature: elearning-app-zimbabwe, Property 23: Marketplace Attribution Completeness — Validates: Requirements 8.5
    [Property(MaxTest = 100)]
    public async Task Property23_marketplace_pack_contains_attribution(byte schoolRaw, byte teacherRaw)
    {
        string schoolName = $"School {schoolRaw}";
        string teacherName = $"Teacher {teacherRaw}";
        using ServiceProvider provider = MarketplacePropertyTestHost.Create();
        using IServiceScope scope = MarketplacePropertyTestHost.CreateScope(provider);
        (EduZimDbContext db, IMediator mediator, MutableCurrentUser current) =
            MarketplacePropertyFlow.Resolve(provider, scope);
        MarketplaceSchools schools = await MarketplacePropertySeeds
            .SeedTwoSchoolsAsync(db, schoolName, teacherName)
            .ConfigureAwait(false);

        Guid packId = await MarketplacePropertyFlow
            .SubmitAndApproveAsync(mediator, current, schools, "Attributed pack")
            .ConfigureAwait(false);
        await MarketplacePropertySeeds
            .SeedApprovedPackAsync(
                db,
                schools.OriginTenantId,
                schools.OriginTeacherId,
                schools.OriginContentItemId,
                "Missing attribution",
                schoolName: " ",
                teacherName: "")
            .ConfigureAwait(false);

        MarketplacePropertyFlow.AsTeacher(current, schools.RequestingTenantId, schools.RequestingTeacherId);
        IReadOnlyList<ContentPackDto> browse = await mediator
            .Send(new BrowseContentPacksQuery(schools.RequestingTenantId), CancellationToken.None)
            .ConfigureAwait(false);
        ContentPackDto dto = Assert.Single(browse);
        Assert.Equal(packId, dto.Id);
        Assert.False(string.IsNullOrWhiteSpace(dto.SchoolName));
        Assert.False(string.IsNullOrWhiteSpace(dto.TeacherName));
        Assert.Equal(schoolName, dto.SchoolName);
        Assert.Equal(teacherName, dto.TeacherName);
    }
}
