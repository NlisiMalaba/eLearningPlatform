using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Commands.SetModuleContentItems;
using EduZim.Application.Content.Queries.GetModuleById;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class SetModuleContentItemsCommandHandlerTests
{
    [Fact]
    public async Task Replaces_module_content_in_the_requested_order()
    {
        Guid tenantId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        List<ModuleContentItem> captured = [];
        SetModuleContentItemsCommandHandler handler = CreateHandler(
            tenantId,
            moduleId,
            [first, second],
            captured);

        await handler.Handle(
            new SetModuleContentItemsCommand(tenantId, moduleId, [second, first]),
            CancellationToken.None);

        Assert.Equal(2, captured.Count);
        Assert.Equal(second, captured[0].ContentItemId);
        Assert.Equal(1, captured[0].SequenceOrder);
        Assert.Equal(first, captured[1].ContentItemId);
    }

    [Fact]
    public async Task Missing_module_is_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        SetModuleContentItemsCommandHandler handler = CreateHandler(
            tenantId,
            Guid.NewGuid(),
            [],
            []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new SetModuleContentItemsCommand(tenantId, Guid.NewGuid(), []),
                CancellationToken.None));
    }

    private static SetModuleContentItemsCommandHandler CreateHandler(
        Guid tenantId,
        Guid moduleId,
        List<Guid> contentIds,
        List<ModuleContentItem> captured)
    {
        List<Module> modules =
        [
            new Module { Id = moduleId, TenantId = tenantId, Title = "Fractions", Subject = "Math" },
        ];
        List<ContentItem> content = contentIds
            .Select(id => new ContentItem
            {
                Id = id,
                TenantId = tenantId,
                Title = id.ToString(),
                Type = ContentType.Pdf,
                Status = ContentStatus.Draft,
                StorageKey = "k",
            })
            .ToList();

        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ContentItems).Returns(content.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<ModuleContentItem>> links = new List<ModuleContentItem>().AsQueryable().BuildMockDbSet();
        links.Setup(s => s.AddAsync(It.IsAny<ModuleContentItem>(), It.IsAny<CancellationToken>()))
            .Callback<ModuleContentItem, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(ValueTask.FromResult<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ModuleContentItem>>(null!));
        db.Setup(x => x.ModuleContentItems).Returns(links.Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Teacher);
        user.Setup(u => u.TenantId).Returns(tenantId);

        Mock<IMediator> mediator = new();
        mediator.Setup(m => m.Send(It.IsAny<GetModuleByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModuleDetailDto { Id = moduleId, Title = "Fractions", Subject = "Math" });

        return new SetModuleContentItemsCommandHandler(
            db.Object,
            user.Object,
            mediator.Object,
            NullLogger<SetModuleContentItemsCommandHandler>.Instance);
    }
}
