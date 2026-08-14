using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Commands.PublishContent;
using EduZim.Application.Content.Queries.GetContentById;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class PublishContentCommandHandlerTests
{
    [Fact]
    public async Task Publishes_draft_content_for_the_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        ContentItem item = Item(tenantId, ContentStatus.Draft);
        PublishContentCommandHandler handler = CreateHandler(tenantId, [item]);

        ContentDetailDto dto = await handler.Handle(
            new PublishContentCommand(tenantId, item.Id),
            CancellationToken.None);

        Assert.Equal(ContentStatus.Published, item.Status);
        Assert.Equal(ContentStatus.Published, dto.Status);
        Assert.Equal("https://cdn.example/file", dto.DownloadUrl);
    }

    [Fact]
    public async Task Archived_content_cannot_be_published()
    {
        Guid tenantId = Guid.NewGuid();
        ContentItem item = Item(tenantId, ContentStatus.Archived);
        PublishContentCommandHandler handler = CreateHandler(tenantId, [item]);

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(new PublishContentCommand(tenantId, item.Id), CancellationToken.None));
    }

    private static ContentItem Item(Guid tenantId, ContentStatus status)
    {
        return new ContentItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Notes",
            Type = ContentType.Pdf,
            Status = status,
            StorageKey = "notes",
            Language = "en",
        };
    }

    private static PublishContentCommandHandler CreateHandler(Guid tenantId, List<ContentItem> items)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.ContentItems).Returns(items.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Teacher);
        user.Setup(u => u.TenantId).Returns(tenantId);

        Mock<IStorageService> storage = new();
        storage.Setup(s => s.GetSignedUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("https://cdn.example/file");

        return new PublishContentCommandHandler(
            db.Object,
            user.Object,
            storage.Object,
            Options.Create(new ContentStorageOptions { SignedUrlExpiryMinutes = 60 }),
            NullLogger<PublishContentCommandHandler>.Instance);
    }
}
