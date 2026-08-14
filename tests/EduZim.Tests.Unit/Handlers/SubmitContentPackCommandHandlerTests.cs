using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.Commands.SubmitContentPack;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using static EduZim.Tests.Unit.Handlers.MarketplaceTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class SubmitContentPackCommandHandlerTests
{
    [Fact]
    public async Task Teacher_submits_pack_with_pending_review_status_and_attribution()
    {
        Guid tenantId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        List<ContentPack> packs = [];
        SubmitContentPackCommandHandler handler = new(
            CreateDb(
                tenants: [School(tenantId, "Mufakose Primary", "Mufakose Primary")],
                users: [Teacher(tenantId, teacherId, "Agnes Moyo")],
                items: [Item(tenantId, itemId)],
                packs: packs).Object,
            CurrentUser(tenantId, UserRole.Teacher, teacherId).Object,
            NullLogger<SubmitContentPackCommandHandler>.Instance);

        ContentPackDto dto = await handler.Handle(
            new SubmitContentPackCommand(tenantId, "Grade 4 Fractions", "Shared lessons", [itemId]),
            CancellationToken.None);

        ContentPack pack = Assert.Single(packs);
        Assert.Equal(ContentPackStatus.PendingReview, pack.Status);
        Assert.Equal(ContentPackStatus.PendingReview, dto.Status);
        Assert.Equal("Mufakose Primary", dto.SchoolName);
        Assert.Equal("Agnes Moyo", dto.TeacherName);
        Assert.False(string.IsNullOrWhiteSpace(dto.SchoolName));
        Assert.False(string.IsNullOrWhiteSpace(dto.TeacherName));
    }

    [Fact]
    public async Task Missing_content_item_throws_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        SubmitContentPackCommandHandler handler = new(
            CreateDb(
                tenants: [School(tenantId, "School", "School")],
                users: [Teacher(tenantId, teacherId, "Agnes Moyo")],
                items: [],
                packs: []).Object,
            CurrentUser(tenantId, UserRole.Teacher, teacherId).Object,
            NullLogger<SubmitContentPackCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new SubmitContentPackCommand(tenantId, "Pack", "Desc", [Guid.NewGuid()]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Missing_teacher_name_throws_domain_exception()
    {
        Guid tenantId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        SubmitContentPackCommandHandler handler = new(
            CreateDb(
                tenants: [School(tenantId, "School", "School")],
                users: [Teacher(tenantId, teacherId, "  ")],
                items: [Item(tenantId, itemId)],
                packs: []).Object,
            CurrentUser(tenantId, UserRole.Teacher, teacherId).Object,
            NullLogger<SubmitContentPackCommandHandler>.Instance);

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new SubmitContentPackCommand(tenantId, "Pack", "Desc", [itemId]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Student_cannot_submit()
    {
        Guid tenantId = Guid.NewGuid();
        SubmitContentPackCommandHandler handler = new(
            CreateDb().Object,
            CurrentUser(tenantId, UserRole.Student, Guid.NewGuid()).Object,
            NullLogger<SubmitContentPackCommandHandler>.Instance);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new SubmitContentPackCommand(tenantId, "Pack", "Desc", [Guid.NewGuid()]),
                CancellationToken.None));
    }
}
