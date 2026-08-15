using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Identity.Commands.UpdateFontSizePreference;
using EduZim.Application.Identity.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class UpdateFontSizePreferenceCommandHandlerTests
{
    [Fact]
    public async Task Updates_own_font_size_preference()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        ApplicationUser user = User(tenantId, userId);
        UpdateFontSizePreferenceCommandHandler handler = CreateHandler(tenantId, userId, [user]);

        FontSizePreferenceDto dto = await handler.Handle(
            new UpdateFontSizePreferenceCommand(userId, "Large"),
            CancellationToken.None);

        Assert.Equal(FontSize.Large, user.FontSize);
        Assert.Equal(userId, dto.UserId);
        Assert.Equal("Large", dto.FontSize);
    }

    [Fact]
    public async Task Student_cannot_update_another_users_font_size()
    {
        Guid tenantId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();
        UpdateFontSizePreferenceCommandHandler handler = CreateHandler(
            tenantId,
            Guid.NewGuid(),
            [User(tenantId, targetUserId)]);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new UpdateFontSizePreferenceCommand(targetUserId, "Small"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Missing_user_is_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        UpdateFontSizePreferenceCommandHandler handler = CreateHandler(tenantId, userId, []);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new UpdateFontSizePreferenceCommand(userId, "Medium"),
                CancellationToken.None));
    }

    private static ApplicationUser User(Guid tenantId, Guid userId)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            Role = UserRole.Student,
            FontSize = FontSize.Medium,
        };
    }

    private static UpdateFontSizePreferenceCommandHandler CreateHandler(
        Guid tenantId,
        Guid currentUserId,
        List<ApplicationUser> users)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> current = new();
        current.Setup(u => u.Role).Returns(UserRole.Student);
        current.Setup(u => u.TenantId).Returns(tenantId);
        current.Setup(u => u.UserId).Returns(currentUserId);

        return new UpdateFontSizePreferenceCommandHandler(
            db.Object,
            current.Object,
            NullLogger<UpdateFontSizePreferenceCommandHandler>.Instance);
    }
}
