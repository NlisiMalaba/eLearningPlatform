using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Queries.GetModuleById;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetModuleByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_content_items_with_type_in_sequence()
    {
        Guid tenantId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        Guid videoId = Guid.NewGuid();
        Guid pdfId = Guid.NewGuid();
        GetModuleByIdQueryHandler handler = CreateHandler(
            tenantId,
            [
                new Module
                {
                    Id = moduleId,
                    TenantId = tenantId,
                    Title = "Fractions",
                    Grade = GradeLevel.Grade2,
                    Subject = "Math",
                    SequenceOrder = 1,
                    IsRequired = true,
                },
            ],
            [
                new ModuleContentItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = moduleId,
                    ContentItemId = pdfId,
                    SequenceOrder = 2,
                },
                new ModuleContentItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ModuleId = moduleId,
                    ContentItemId = videoId,
                    SequenceOrder = 1,
                },
            ],
            [
                new ContentItem
                {
                    Id = videoId,
                    TenantId = tenantId,
                    Title = "Intro video",
                    Type = ContentType.Video,
                    StorageKey = "video",
                },
                new ContentItem
                {
                    Id = pdfId,
                    TenantId = tenantId,
                    Title = "Worksheet",
                    Type = ContentType.Pdf,
                    StorageKey = "pdf",
                },
            ]);

        ModuleDetailDto dto = await handler.Handle(
            new GetModuleByIdQuery(tenantId, moduleId),
            CancellationToken.None);

        Assert.Equal("Fractions", dto.Title);
        Assert.Equal(2, dto.ContentItems.Count);
        Assert.Equal(videoId, dto.ContentItems[0].ContentItemId);
        Assert.Equal(ContentType.Video, dto.ContentItems[0].Type);
        Assert.Equal(ContentType.Pdf, dto.ContentItems[1].Type);
    }

    [Fact]
    public async Task Missing_module_is_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        GetModuleByIdQueryHandler handler = CreateHandler(tenantId, [], [], []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetModuleByIdQuery(tenantId, Guid.NewGuid()), CancellationToken.None));
    }

    private static GetModuleByIdQueryHandler CreateHandler(
        Guid tenantId,
        List<Module> modules,
        List<ModuleContentItem> links,
        List<ContentItem> contentItems)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ModuleContentItems).Returns(links.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ContentItems).Returns(contentItems.AsQueryable().BuildMockDbSet().Object);
        return new GetModuleByIdQueryHandler(db.Object);
    }
}
