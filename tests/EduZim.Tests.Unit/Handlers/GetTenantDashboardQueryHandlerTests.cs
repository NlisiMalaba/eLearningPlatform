using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants.Models;
using EduZim.Application.Tenants.Queries.GetTenantDashboard;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetTenantDashboardQueryHandlerTests
{
    [Fact]
    public async Task Returns_enrolment_teacher_subscription_and_storage_totals()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Tenants).Returns(new List<Tenant>
        {
            new() { Id = tenantId, Name = "School" },
        }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Role = UserRole.Student },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Role = UserRole.Student },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, Role = UserRole.Teacher },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Role = UserRole.Teacher,
                LockoutEnd = DateTimeOffset.UtcNow.AddHours(1),
            },
        }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Subscriptions).Returns(new List<Subscription>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Status = SubscriptionStatus.Active,
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(20),
            },
        }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.ContentItems).Returns(new List<ContentItem>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, FileSizeBytes = 100, StorageKey = "a" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, FileSizeBytes = 50, StorageKey = "b" },
        }.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.SchoolAdmin);
        user.Setup(u => u.TenantId).Returns(tenantId);

        GetTenantDashboardQueryHandler handler = new(db.Object, user.Object);
        TenantDashboardDto dto = await handler.Handle(new GetTenantDashboardQuery(tenantId), CancellationToken.None);

        Assert.Equal(2, dto.EnrolledStudentsCount);
        Assert.Equal(1, dto.ActiveTeachersCount);
        Assert.Equal(SubscriptionStatus.Active, dto.SubscriptionStatus);
        Assert.Equal(150, dto.StorageUsageBytes);
    }
}
