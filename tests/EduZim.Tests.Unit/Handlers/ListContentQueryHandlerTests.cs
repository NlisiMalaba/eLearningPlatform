using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Queries.ListContent;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ListContentQueryHandlerTests
{
    [Fact]
    public async Task Returns_non_archived_content_for_the_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        Guid keepId = Guid.NewGuid();
        ListContentQueryHandler handler = CreateHandler(
            tenantId,
            [
                Item(tenantId, keepId, "Algebra video", ContentStatus.Draft),
                Item(tenantId, Guid.NewGuid(), "Old", ContentStatus.Archived),
                Item(Guid.NewGuid(), Guid.NewGuid(), "Other school", ContentStatus.Published),
            ]);

        IReadOnlyList<ContentListItemDto> items = await handler.Handle(
            new ListContentQuery(tenantId),
            CancellationToken.None);

        ContentListItemDto dto = Assert.Single(items);
        Assert.Equal(keepId, dto.Id);
        Assert.Equal("Algebra video", dto.Title);
    }

    [Fact]
    public async Task Student_cannot_list_content()
    {
        Guid tenantId = Guid.NewGuid();
        ListContentQueryHandler handler = CreateHandler(tenantId, [], UserRole.Student);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new ListContentQuery(tenantId), CancellationToken.None));
    }

    private static ContentItem Item(Guid tenantId, Guid id, string title, ContentStatus status)
    {
        return new ContentItem
        {
            Id = id,
            TenantId = tenantId,
            Title = title,
            Type = ContentType.Video,
            Status = status,
            StorageKey = "key",
            FileSizeBytes = 10,
        };
    }

    private static ListContentQueryHandler CreateHandler(
        Guid tenantId,
        List<ContentItem> items,
        UserRole role = UserRole.Teacher)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.ContentItems).Returns(items.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(Guid.NewGuid());
        return new ListContentQueryHandler(db.Object, user.Object);
    }
}
