using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.Commands.ApproveAccess;
using EduZim.Application.Marketplace.Commands.RequestAccess;
using EduZim.Application.Marketplace.Queries.GetContentPack;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static EduZim.Tests.Unit.Handlers.MarketplaceTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class RequestAccessCommandHandlerTests
{
    [Fact]
    public async Task Request_notifies_originating_teacher()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        List<ContentPackAccessRequest> requests = [];
        List<QueueNotificationCommand> queued = [];
        RequestAccessCommandHandler handler = new(
            CreateDb(
                packs: [Pack(originTenant, packId, teacherId, ContentPackStatus.Approved)],
                requests: requests).Object,
            CurrentUser(callerTenant, UserRole.Teacher, Guid.NewGuid()).Object,
            Mediator(queued).Object,
            NullLogger<RequestAccessCommandHandler>.Instance);

        Guid requestId = await handler.Handle(
            new RequestAccessCommand(callerTenant, packId),
            CancellationToken.None);

        ContentPackAccessRequest row = Assert.Single(requests);
        Assert.Equal(requestId, row.Id);
        Assert.Equal(ContentPackAccessStatus.Pending, row.Status);
        Assert.Equal(originTenant, row.TenantId);
        Assert.Equal(callerTenant, row.RequestingTenantId);
        QueueNotificationCommand notice = Assert.Single(queued);
        Assert.Equal(teacherId, notice.UserId);
        Assert.Equal(NotificationType.MarketplaceAccessRequested, notice.Type);
    }

    [Fact]
    public async Task Pending_pack_is_not_found()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        RequestAccessCommandHandler handler = new(
            CreateDb(packs: [Pack(originTenant, packId, Guid.NewGuid(), ContentPackStatus.PendingReview)])
                .Object,
            CurrentUser(callerTenant, UserRole.Teacher, Guid.NewGuid()).Object,
            Mediator([]).Object,
            NullLogger<RequestAccessCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new RequestAccessCommand(callerTenant, packId), CancellationToken.None));
    }

    [Fact]
    public async Task Duplicate_pending_request_conflicts()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        RequestAccessCommandHandler handler = new(
            CreateDb(
                packs: [Pack(originTenant, packId, Guid.NewGuid(), ContentPackStatus.Approved)],
                requests:
                [
                    AccessRequest(originTenant, packId, callerTenant, Guid.NewGuid(), ContentPackAccessStatus.Pending)
                ]).Object,
            CurrentUser(callerTenant, UserRole.Teacher, Guid.NewGuid()).Object,
            Mediator([]).Object,
            NullLogger<RequestAccessCommandHandler>.Instance);

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new RequestAccessCommand(callerTenant, packId), CancellationToken.None));
    }

    private static Mock<IMediator> Mediator(List<QueueNotificationCommand> queued)
    {
        Mock<IMediator> mediator = new();
        mediator
            .Setup(m => m.Send(It.IsAny<QueueNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<MediatR.Unit>, CancellationToken>((req, _) => queued.Add((QueueNotificationCommand)req))
            .ReturnsAsync(MediatR.Unit.Value);
        return mediator;
    }
}

public sealed class ApproveAccessCommandHandlerTests
{
    [Fact]
    public async Task Originating_teacher_grants_access_to_requesting_tenant_only()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid otherTenant = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        ContentPackAccessRequest pending = AccessRequest(
            originTenant,
            packId,
            callerTenant,
            Guid.NewGuid(),
            ContentPackAccessStatus.Pending);
        ContentPackAccessRequest otherPending = AccessRequest(
            originTenant,
            packId,
            otherTenant,
            Guid.NewGuid(),
            ContentPackAccessStatus.Pending);
        ApproveAccessCommandHandler handler = new(
            CreateDb(
                packs: [Pack(originTenant, packId, teacherId, ContentPackStatus.Approved)],
                requests: [pending, otherPending]).Object,
            CurrentUser(originTenant, UserRole.Teacher, teacherId).Object,
            NullLogger<ApproveAccessCommandHandler>.Instance);

        await handler.Handle(
            new ApproveAccessCommand(originTenant, packId, callerTenant),
            CancellationToken.None);

        Assert.Equal(ContentPackAccessStatus.Approved, pending.Status);
        Assert.Equal(ContentPackAccessStatus.Pending, otherPending.Status);
        Assert.NotNull(pending.ReviewedAtUtc);
    }

    [Fact]
    public async Task Unrelated_teacher_cannot_approve_access()
    {
        Guid originTenant = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        ApproveAccessCommandHandler handler = new(
            CreateDb(packs: [Pack(originTenant, packId, Guid.NewGuid(), ContentPackStatus.Approved)]).Object,
            CurrentUser(originTenant, UserRole.Teacher, Guid.NewGuid()).Object,
            NullLogger<ApproveAccessCommandHandler>.Instance);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new ApproveAccessCommand(originTenant, packId, Guid.NewGuid()),
                CancellationToken.None));
    }
}

public sealed class GetContentPackQueryHandlerTests
{
    [Fact]
    public async Task ContentAccess_RequiresOriginatingTeacherApproval()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        Guid itemId = Guid.NewGuid();
        ContentPack pack = Pack(originTenant, packId, Guid.NewGuid(), ContentPackStatus.Approved);
        GetContentPackQueryHandler handler = CreateGetHandler(
            callerTenant,
            [pack],
            [PackItem(originTenant, packId, itemId)],
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetContentPackQuery(callerTenant, packId), CancellationToken.None));

        ContentPackAccessRequest grant = AccessRequest(
            originTenant,
            packId,
            callerTenant,
            Guid.NewGuid(),
            ContentPackAccessStatus.Approved);
        GetContentPackQueryHandler allowed = CreateGetHandler(
            callerTenant,
            [pack],
            [PackItem(originTenant, packId, itemId)],
            [grant]);

        Application.Marketplace.DTOs.ContentPackDetailDto detail = await allowed.Handle(
            new GetContentPackQuery(callerTenant, packId),
            CancellationToken.None);

        Assert.Equal(itemId, Assert.Single(detail.ContentItemIds));
        Assert.True(detail.Pack.HasAccess);
        Assert.Equal("Mufakose Primary", detail.Pack.SchoolName);
        Assert.Equal("Agnes Moyo", detail.Pack.TeacherName);
    }

    [Fact]
    public async Task Pending_pack_is_hidden_from_other_tenants()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        GetContentPackQueryHandler handler = CreateGetHandler(
            callerTenant,
            [Pack(originTenant, packId, Guid.NewGuid(), ContentPackStatus.PendingReview)],
            [],
            []);

        await Assert.ThrowsAsync<Application.Exceptions.NotFoundException>(
            () => handler.Handle(new GetContentPackQuery(callerTenant, packId), CancellationToken.None));
    }

    [Fact]
    public async Task Approved_access_does_not_expose_another_pack()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid grantedPackId = Guid.NewGuid();
        Guid otherPackId = Guid.NewGuid();
        Guid grantedItem = Guid.NewGuid();
        Guid otherItem = Guid.NewGuid();
        GetContentPackQueryHandler handler = CreateGetHandler(
            callerTenant,
            [
                Pack(originTenant, grantedPackId, Guid.NewGuid(), ContentPackStatus.Approved, title: "Granted"),
                Pack(originTenant, otherPackId, Guid.NewGuid(), ContentPackStatus.Approved, title: "Other"),
            ],
            [
                PackItem(originTenant, grantedPackId, grantedItem),
                PackItem(originTenant, otherPackId, otherItem),
            ],
            [
                AccessRequest(originTenant, grantedPackId, callerTenant, Guid.NewGuid(), ContentPackAccessStatus.Approved)
            ]);

        Application.Marketplace.DTOs.ContentPackDetailDto granted = await handler.Handle(
            new GetContentPackQuery(callerTenant, grantedPackId),
            CancellationToken.None);
        Assert.Equal(grantedItem, Assert.Single(granted.ContentItemIds));

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetContentPackQuery(callerTenant, otherPackId), CancellationToken.None));
    }

    private static GetContentPackQueryHandler CreateGetHandler(
        Guid callerTenant,
        List<ContentPack> packs,
        List<ContentPackItem> items,
        List<ContentPackAccessRequest> requests)
    {
        return new GetContentPackQueryHandler(
            CreateDb(packs: packs, packItems: items, requests: requests).Object,
            CurrentUser(callerTenant, UserRole.Teacher, Guid.NewGuid()).Object,
            NullLogger<GetContentPackQueryHandler>.Instance);
    }
}
