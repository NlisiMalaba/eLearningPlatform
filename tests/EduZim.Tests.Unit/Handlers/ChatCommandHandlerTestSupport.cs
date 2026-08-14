using EduZim.Application.Common.Interfaces;
using EduZim.Application.ZimBot.Commands.Chat;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

internal static class ChatCommandHandlerTestSupport
{
    public static ApplicationUser Student(Guid tenantId, Guid userId, string language)
    {
        return new ApplicationUser
        {
            Id = userId,
            TenantId = tenantId,
            Role = UserRole.Student,
            PreferredLanguage = language,
        };
    }

    public static Module Module(Guid tenantId, Guid moduleId, string title, string subject, GradeLevel grade)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new Module
        {
            Id = moduleId,
            TenantId = tenantId,
            Title = title,
            Subject = subject,
            Grade = grade,
            SequenceOrder = 1,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static StudentProgress Progress(Guid tenantId, Guid studentId, Guid moduleId, bool isCompleted)
    {
        DateTime utcNow = DateTime.UtcNow;
        return new StudentProgress
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StudentId = studentId,
            ModuleId = moduleId,
            IsCompleted = isCompleted,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ChatCommandHandler CreateHandler(
        Guid tenantId,
        UserRole role,
        Guid currentUserId,
        List<ApplicationUser> users,
        List<Module> modules,
        List<StudentProgress> progress,
        List<ZimBotInteraction> captured,
        Mock<IAiService> ai)
    {
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Users).Returns(users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Modules).Returns(modules.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.StudentProgresses).Returns(progress.AsQueryable().BuildMockDbSet().Object);

        Mock<DbSet<ZimBotInteraction>> interactions = new List<ZimBotInteraction>().AsQueryable().BuildMockDbSet();
        interactions.Setup(s => s.AddAsync(It.IsAny<ZimBotInteraction>(), It.IsAny<CancellationToken>()))
            .Callback<ZimBotInteraction, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ZimBotInteraction>>(
                (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<ZimBotInteraction>)null!));
        db.Setup(x => x.ZimBotInteractions).Returns(interactions.Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(role);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(currentUserId);

        return new ChatCommandHandler(
            db.Object,
            user.Object,
            ai.Object,
            new Mock<IPublisher>().Object,
            NullLogger<ChatCommandHandler>.Instance);
    }
}
