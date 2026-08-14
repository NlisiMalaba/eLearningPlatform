using EduZim.Application.Common.Interfaces;
using EduZim.Application.ZimBot.DTOs;
using EduZim.Application.ZimBot.Queries.GetInteractionLogs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetInteractionLogsQueryHandlerTests
{
    [Fact]
    public async Task Teacher_sees_tenant_logs_newest_first()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        DateTime older = DateTime.UtcNow.AddHours(-2);
        DateTime newer = DateTime.UtcNow;
        GetInteractionLogsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            Guid.NewGuid(),
            [
                Row(tenantId, studentId, "old", older),
                Row(tenantId, studentId, "new", newer),
                Row(Guid.NewGuid(), studentId, "other tenant", newer),
            ]);

        ZimBotInteractionLogsDto dto = await handler.Handle(
            new GetInteractionLogsQuery(tenantId, null),
            CancellationToken.None);

        Assert.Equal(2, dto.Items.Count);
        Assert.Equal("new", dto.Items[0].Question);
        Assert.Equal("old", dto.Items[1].Question);
    }

    [Fact]
    public async Task Student_filter_returns_only_that_student()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentA = Guid.NewGuid();
        Guid studentB = Guid.NewGuid();
        GetInteractionLogsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.SchoolAdmin,
            Guid.NewGuid(),
            [
                Row(tenantId, studentA, "a", DateTime.UtcNow),
                Row(tenantId, studentB, "b", DateTime.UtcNow),
            ]);

        ZimBotInteractionLogsDto dto = await handler.Handle(
            new GetInteractionLogsQuery(tenantId, studentA),
            CancellationToken.None);

        ZimBotInteractionLogDto item = Assert.Single(dto.Items);
        Assert.Equal(studentA, item.StudentId);
    }

    [Fact]
    public async Task Student_cannot_review_logs()
    {
        Guid tenantId = Guid.NewGuid();
        GetInteractionLogsQueryHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            Guid.NewGuid(),
            []);

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new GetInteractionLogsQuery(tenantId, null), CancellationToken.None));
    }

    private static ZimBotInteraction Row(Guid tenantId, Guid studentId, string question, DateTime createdAt)
    {
        return new ZimBotInteraction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            Question = question,
            Response = "reply",
            Language = "English",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    private static GetInteractionLogsQueryHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid currentUserId,
        List<ZimBotInteraction> rows)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.ZimBotInteractions).Returns(rows.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(currentUserId);

        return new GetInteractionLogsQueryHandler(
            db.Object,
            user.Object,
            NullLogger<GetInteractionLogsQueryHandler>.Instance);
    }
}
