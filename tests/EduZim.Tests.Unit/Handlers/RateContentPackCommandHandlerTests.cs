using EduZim.Application.Exceptions;
using EduZim.Application.Marketplace.Commands.RateContentPack;
using EduZim.Application.Marketplace.Commands.RemoveContentPack;
using EduZim.Application.Marketplace.DTOs;
using EduZim.Application.Notifications.Commands.QueueNotification;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static EduZim.Tests.Unit.Handlers.MarketplaceTestData;

namespace EduZim.Tests.Unit.Handlers;

public sealed class RateContentPackCommandHandlerTests
{
    [Fact]
    public async Task Teacher_can_rate_and_review_an_approved_pack()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        List<ContentPackRating> ratings = [];
        RateContentPackCommandHandler handler = new(
            CreateDb(
                packs: [Pack(originTenant, packId, Guid.NewGuid(), ContentPackStatus.Approved)],
                ratings: ratings).Object,
            CurrentUser(callerTenant, UserRole.Teacher, userId).Object,
            NullLogger<RateContentPackCommandHandler>.Instance);

        ContentPackDto dto = await handler.Handle(
            new RateContentPackCommand(callerTenant, packId, 5, "Excellent Shona explanations"),
            CancellationToken.None);

        ContentPackRating row = Assert.Single(ratings);
        Assert.Equal(5, row.Rating);
        Assert.Equal("Excellent Shona explanations", row.Review);
        Assert.Equal(5, dto.AverageRating);
        Assert.Equal(1, dto.RatingCount);
    }

    [Fact]
    public async Task Existing_rating_is_updated()
    {
        Guid originTenant = Guid.NewGuid();
        Guid callerTenant = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        ContentPackRating existing = Rating(callerTenant, packId, userId, 2);
        RateContentPackCommandHandler handler = new(
            CreateDb(
                packs: [Pack(originTenant, packId, Guid.NewGuid(), ContentPackStatus.Approved)],
                ratings: [existing]).Object,
            CurrentUser(callerTenant, UserRole.Teacher, userId).Object,
            NullLogger<RateContentPackCommandHandler>.Instance);

        ContentPackDto dto = await handler.Handle(
            new RateContentPackCommand(callerTenant, packId, 4, null),
            CancellationToken.None);

        Assert.Equal(4, existing.Rating);
        Assert.Equal(4, dto.AverageRating);
        Assert.Equal(1, dto.RatingCount);
    }

    [Fact]
    public async Task Cannot_rate_own_school_pack()
    {
        Guid tenantId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        RateContentPackCommandHandler handler = new(
            CreateDb(packs: [Pack(tenantId, packId, Guid.NewGuid(), ContentPackStatus.Approved)]).Object,
            CurrentUser(tenantId, UserRole.Teacher, Guid.NewGuid()).Object,
            NullLogger<RateContentPackCommandHandler>.Instance);

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(
                new RateContentPackCommand(tenantId, packId, 5, null),
                CancellationToken.None));
    }
}

public sealed class RemoveContentPackCommandHandlerTests
{
    [Fact]
    public async Task Platform_admin_removes_pack_and_notifies_submitting_teacher()
    {
        Guid tenantId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();
        ContentPack pack = Pack(tenantId, packId, teacherId, ContentPackStatus.Approved);
        List<QueueNotificationCommand> queued = [];
        RemoveContentPackCommandHandler handler = new(
            CreateDb(packs: [pack]).Object,
            CurrentUser(null, UserRole.PlatformAdmin, Guid.NewGuid()).Object,
            Mediator(queued).Object,
            NullLogger<RemoveContentPackCommandHandler>.Instance);

        await handler.Handle(new RemoveContentPackCommand(packId), CancellationToken.None);

        Assert.Equal(ContentPackStatus.Removed, pack.Status);
        Assert.NotNull(pack.RemovedAtUtc);
        QueueNotificationCommand notice = Assert.Single(queued);
        Assert.Equal(teacherId, notice.UserId);
        Assert.Equal(NotificationType.MarketplacePackRemoved, notice.Type);
    }

    [Fact]
    public async Task Teacher_cannot_remove_pack()
    {
        RemoveContentPackCommandHandler handler = new(
            CreateDb().Object,
            CurrentUser(Guid.NewGuid(), UserRole.Teacher, Guid.NewGuid()).Object,
            Mediator([]).Object,
            NullLogger<RemoveContentPackCommandHandler>.Instance);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new RemoveContentPackCommand(Guid.NewGuid()), CancellationToken.None));
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
