using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Queries.ListModules;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ListModulesQueryHandlerTests
{
    [Fact]
    public async Task Returns_modules_with_content_counts()
    {
        Guid tenantId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Modules).Returns(new List<Module>
        {
            new()
            {
                Id = moduleId,
                TenantId = tenantId,
                Title = "Fractions",
                Subject = "Math",
                Grade = GradeLevel.Grade2,
                SequenceOrder = 1,
            },
        }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ModuleContentItems).Returns(new List<ModuleContentItem>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, ModuleId = moduleId, ContentItemId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, ModuleId = moduleId, ContentItemId = Guid.NewGuid() },
        }.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Teacher);
        user.Setup(u => u.TenantId).Returns(tenantId);

        ListModulesQueryHandler handler = new(db.Object, user.Object);
        IReadOnlyList<ModuleListItemDto> items = await handler.Handle(
            new ListModulesQuery(tenantId),
            CancellationToken.None);

        ModuleListItemDto dto = Assert.Single(items);
        Assert.Equal("Fractions", dto.Title);
        Assert.Equal(2, dto.ContentItemCount);
    }
}
