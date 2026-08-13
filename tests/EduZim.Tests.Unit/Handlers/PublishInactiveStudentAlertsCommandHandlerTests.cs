using EduZim.Application.Common.Interfaces;
using EduZim.Application.Notifications.Commands.PublishInactiveStudentAlerts;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class PublishInactiveStudentAlertsCommandHandlerTests
{
    [Fact]
    public async Task Publishes_for_students_inactive_for_seven_days()
    {
        Guid tenantId = Guid.NewGuid();
        Guid inactiveId = Guid.NewGuid();
        DateTime lastLogin = DateTime.UtcNow.AddDays(-8);
        List<StudentInactiveNotification> published = [];
        PublishInactiveStudentAlertsCommandHandler handler = CreateHandler(
            [ActiveTenant(tenantId)],
            [
                Student(tenantId, inactiveId, lastLogin, lastAlertAt: null),
                Student(tenantId, Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), lastAlertAt: null),
            ],
            published);

        await handler.Handle(new PublishInactiveStudentAlertsCommand(), CancellationToken.None);

        StudentInactiveNotification publishedItem = Assert.Single(published);
        Assert.Equal(inactiveId, publishedItem.StudentId);
        Assert.Equal(tenantId, publishedItem.TenantId);
        Assert.Equal(lastLogin, publishedItem.LastLoginAt);
    }

    [Fact]
    public async Task Skips_students_already_alerted_since_last_login()
    {
        Guid tenantId = Guid.NewGuid();
        List<StudentInactiveNotification> published = [];
        DateTime lastLogin = DateTime.UtcNow.AddDays(-10);
        PublishInactiveStudentAlertsCommandHandler handler = CreateHandler(
            [ActiveTenant(tenantId)],
            [Student(tenantId, Guid.NewGuid(), lastLogin, lastAlertAt: lastLogin.AddDays(1))],
            published);

        await handler.Handle(new PublishInactiveStudentAlertsCommand(), CancellationToken.None);

        Assert.Empty(published);
    }

    [Fact]
    public async Task Skips_non_students_and_null_last_login()
    {
        Guid tenantId = Guid.NewGuid();
        List<StudentInactiveNotification> published = [];
        ApplicationUser teacher = Student(tenantId, Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), lastAlertAt: null);
        teacher.Role = UserRole.Teacher;
        ApplicationUser neverLoggedIn = Student(tenantId, Guid.NewGuid(), lastLoginAt: null, lastAlertAt: null);
        PublishInactiveStudentAlertsCommandHandler handler = CreateHandler(
            [ActiveTenant(tenantId)],
            [teacher, neverLoggedIn],
            published);

        await handler.Handle(new PublishInactiveStudentAlertsCommand(), CancellationToken.None);

        Assert.Empty(published);
    }

    [Fact]
    public async Task Skips_suspended_tenants()
    {
        Guid tenantId = Guid.NewGuid();
        List<StudentInactiveNotification> published = [];
        Tenant suspended = ActiveTenant(tenantId);
        suspended.Status = TenantStatus.Suspended;
        PublishInactiveStudentAlertsCommandHandler handler = CreateHandler(
            [suspended],
            [Student(tenantId, Guid.NewGuid(), DateTime.UtcNow.AddDays(-20), lastAlertAt: null)],
            published);

        await handler.Handle(new PublishInactiveStudentAlertsCommand(), CancellationToken.None);

        Assert.Empty(published);
    }

    private static Tenant ActiveTenant(Guid tenantId)
    {
        return new Tenant
        {
            Id = tenantId,
            Name = "School",
            Tier = TenantTier.School,
            Status = TenantStatus.Active,
            Branding = new BrandingSettings { SchoolName = "School", PrimaryColour = "#1976D2" },
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static ApplicationUser Student(
        Guid tenantId,
        Guid userId,
        DateTime? lastLoginAt,
        DateTime? lastAlertAt)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            Role = UserRole.Student,
            LastLoginAt = lastLoginAt,
            LastInactivityAlertAt = lastAlertAt,
        };
    }

    private static PublishInactiveStudentAlertsCommandHandler CreateHandler(
        List<Tenant> tenants,
        List<ApplicationUser> users,
        List<StudentInactiveNotification> published)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Tenants).Returns(tenants.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);

        Mock<IPublisher> publisher = new();
        publisher
            .Setup(p => p.Publish(It.IsAny<StudentInactiveNotification>(), It.IsAny<CancellationToken>()))
            .Callback<StudentInactiveNotification, CancellationToken>((n, _) => published.Add(n))
            .Returns(Task.CompletedTask);

        return new PublishInactiveStudentAlertsCommandHandler(
            db.Object,
            publisher.Object,
            NullLogger<PublishInactiveStudentAlertsCommandHandler>.Instance);
    }
}
