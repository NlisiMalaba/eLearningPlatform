using EduZim.Application.Marketplace.Commands.ApproveContentPack;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Marketplace.Queries.BrowseContentPacks;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using static EduZim.Tests.Unit.Handlers.MarketplaceTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ApproveContentPackCommandHandlerTests
{
    [Fact]
    public async Task Platform_admin_approves_pending_pack()
    {
        Guid tenantId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        ContentPack pack = Pack(tenantId, packId, Guid.NewGuid(), ContentPackStatus.PendingReview);
        ApproveContentPackCommandHandler handler = new(
            CreateDb(packs: [pack]).Object,
            CurrentUser(null, UserRole.PlatformAdmin, Guid.NewGuid()).Object,
            NullLogger<ApproveContentPackCommandHandler>.Instance);

        ContentPackDto dto = await handler.Handle(new ApproveContentPackCommand(packId), CancellationToken.None);

        Assert.Equal(ContentPackStatus.Approved, pack.Status);
        Assert.Equal(ContentPackStatus.Approved, dto.Status);
        Assert.Equal("Mufakose Primary", dto.SchoolName);
        Assert.Equal("Agnes Moyo", dto.TeacherName);
    }

    [Fact]
    public async Task Teacher_cannot_approve_pack()
    {
        Guid tenantId = Guid.NewGuid();
        ApproveContentPackCommandHandler handler = new(
            CreateDb(packs: [Pack(tenantId, Guid.NewGuid(), Guid.NewGuid(), ContentPackStatus.PendingReview)])
                .Object,
            CurrentUser(tenantId, UserRole.Teacher, Guid.NewGuid()).Object,
            NullLogger<ApproveContentPackCommandHandler>.Instance);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new ApproveContentPackCommand(Guid.NewGuid()), CancellationToken.None));
    }
}

public sealed class BrowseContentPacksQueryHandlerTests
{
    [Fact]
    public async Task SubmittedPack_NotDiscoverableUntilApproved()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid pendingId = Guid.NewGuid();
        Guid approvedId = Guid.NewGuid();
        List<ContentPack> packs =
        [
            Pack(originTenant, pendingId, Guid.NewGuid(), ContentPackStatus.PendingReview),
            Pack(originTenant, approvedId, Guid.NewGuid(), ContentPackStatus.Approved, title: "Approved pack"),
            Pack(originTenant, Guid.NewGuid(), Guid.NewGuid(), ContentPackStatus.Removed),
        ];
        BrowseContentPacksQueryHandler handler = new(
            CreateDb(packs: packs).Object,
            CurrentUser(callerTenant, UserRole.Teacher, Guid.NewGuid()).Object,
            NullLogger<BrowseContentPacksQueryHandler>.Instance);

        IReadOnlyList<ContentPackDto> result = await handler.Handle(
            new BrowseContentPacksQuery(callerTenant),
            CancellationToken.None);

        ContentPackDto dto = Assert.Single(result);
        Assert.Equal(approvedId, dto.Id);
        Assert.Equal(ContentPackStatus.Approved, dto.Status);
        Assert.False(string.IsNullOrWhiteSpace(dto.SchoolName));
        Assert.False(string.IsNullOrWhiteSpace(dto.TeacherName));
    }

    [Fact]
    public async Task MarketplacePack_ContainsAttribution()
    {
        Guid callerTenant = Guid.NewGuid();
        ContentPack complete = Pack(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContentPackStatus.Approved,
            schoolName: "Harare High",
            teacherName: "Tinashe Ncube");
        ContentPack missingName = Pack(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContentPackStatus.Approved,
            schoolName: "",
            teacherName: "Someone");
        BrowseContentPacksQueryHandler handler = new(
            CreateDb(packs: [complete, missingName]).Object,
            CurrentUser(callerTenant, UserRole.Teacher, Guid.NewGuid()).Object,
            NullLogger<BrowseContentPacksQueryHandler>.Instance);

        IReadOnlyList<ContentPackDto> result = await handler.Handle(
            new BrowseContentPacksQuery(callerTenant),
            CancellationToken.None);

        ContentPackDto dto = Assert.Single(result);
        Assert.Equal("Harare High", dto.SchoolName);
        Assert.Equal("Tinashe Ncube", dto.TeacherName);
    }

    [Fact]
    public async Task Student_cannot_browse()
    {
        Guid tenantId = Guid.NewGuid();
        BrowseContentPacksQueryHandler handler = new(
            CreateDb().Object,
            CurrentUser(tenantId, UserRole.Student, Guid.NewGuid()).Object,
            NullLogger<BrowseContentPacksQueryHandler>.Instance);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new BrowseContentPacksQuery(tenantId), CancellationToken.None));
    }
}
