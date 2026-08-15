using EduZim.Application.Common.Interfaces;
using EduZim.Application.Assessments.Queries.ListSchoolClasses;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ListSchoolClassesQueryHandlerTests
{
    [Fact]
    public async Task Returns_classes_for_the_current_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SchoolClasses).Returns(new List<SchoolClass>
        {
            new() { Id = classId, TenantId = tenantId, Name = "2A" },
            new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Name = "Other" },
        }.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Teacher);
        user.Setup(u => u.TenantId).Returns(tenantId);

        ListSchoolClassesQueryHandler handler = new(db.Object, user.Object);
        IReadOnlyList<SchoolClassListItemDto> items = await handler.Handle(
            new ListSchoolClassesQuery(tenantId),
            CancellationToken.None);

        SchoolClassListItemDto dto = Assert.Single(items);
        Assert.Equal(classId, dto.Id);
        Assert.Equal("2A", dto.Name);
    }

    [Fact]
    public async Task Student_cannot_list_classes()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<IEduZimDbContext> db = new();
        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Student);
        user.Setup(u => u.TenantId).Returns(tenantId);

        ListSchoolClassesQueryHandler handler = new(db.Object, user.Object);
        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new ListSchoolClassesQuery(tenantId), CancellationToken.None));
    }
}
