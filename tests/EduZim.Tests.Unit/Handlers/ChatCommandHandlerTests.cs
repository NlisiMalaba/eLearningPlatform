using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.ZimBot.Commands.Chat;
using EduZim.Application.ZimBot.DTOs;
using EduZim.Application.ZimBot.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using Moq;
using static EduZim.Tests.Unit.Handlers.ChatCommandHandlerTestSupport;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ChatCommandHandlerTests
{
    [Fact]
    public async Task Includes_grade_module_and_language_in_system_prompt()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        string? prompt = null;
        Mock<IAiService> ai = new();
        ai.Setup(s => s.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((system, _, _) => prompt = system)
            .ReturnsAsync("CONFIDENCE:HIGH\nFractions split a whole into equal parts.");
        ChatCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Student(tenantId, studentId, "sn")],
            [Module(tenantId, moduleId, "Fractions", "Mathematics", GradeLevel.Grade4)],
            [],
            [],
            ai);

        await handler.Handle(
            new ChatCommand(tenantId, studentId, "What is a fraction?", moduleId, false),
            CancellationToken.None);

        Assert.NotNull(prompt);
        Assert.Contains("Shona", prompt, StringComparison.Ordinal);
        Assert.Contains("Grade 4", prompt, StringComparison.Ordinal);
        Assert.Contains("Fractions", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chat_language_override_wins_over_preferred_language()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        string? prompt = null;
        Mock<IAiService> ai = new();
        ai.Setup(s => s.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((system, _, _) => prompt = system)
            .ReturnsAsync("CONFIDENCE:HIGH\nA verb names an action.");
        ChatCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Student(tenantId, studentId, "en")],
            [],
            [],
            [],
            ai);

        ZimBotChatDto dto = await handler.Handle(
            new ChatCommand(tenantId, studentId, "Help with verbs", null, false, "Ndebele"),
            CancellationToken.None);

        Assert.NotNull(prompt);
        Assert.Contains("Ndebele", prompt, StringComparison.Ordinal);
        Assert.Equal(ZimBotLanguage.Ndebele, dto.Language);
    }

    [Fact]
    public async Task Answer_request_enables_hint_mode_and_persists_interaction()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        List<ZimBotInteraction> captured = [];
        Mock<IAiService> ai = new();
        ai.Setup(s => s.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CONFIDENCE:HIGH\nThink about what the question is asking first.");
        ChatCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Student(tenantId, studentId, "en")],
            [],
            [],
            captured,
            ai);

        ZimBotChatDto dto = await handler.Handle(
            new ChatCommand(tenantId, studentId, "What is the answer to question 2?", null, false),
            CancellationToken.None);

        ZimBotInteraction row = Assert.Single(captured);
        Assert.True(row.UsedHintMode);
        Assert.Equal(row.Id, dto.InteractionId);
        Assert.False(dto.UsedFallback);
        ai.Verify(
            s => s.ChatAsync(It.Is<string>(p => p.Contains("HINT MODE", StringComparison.Ordinal)), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Fallback_reply_is_persisted_when_ai_is_unavailable()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        List<ZimBotInteraction> captured = [];
        Mock<IAiService> ai = new();
        ai.Setup(s => s.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ZimBotMessages.Unavailable);
        ChatCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Student(tenantId, studentId, "en")],
            [],
            [],
            captured,
            ai);

        ZimBotChatDto dto = await handler.Handle(
            new ChatCommand(tenantId, studentId, "Help me with verbs", null, false),
            CancellationToken.None);

        Assert.True(dto.UsedFallback);
        Assert.Equal(ZimBotMessages.Unavailable, dto.Reply);
        Assert.True(Assert.Single(captured).UsedFallback);
    }

    [Fact]
    public async Task Low_confidence_empty_body_suggests_asking_the_teacher()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Mock<IAiService> ai = new();
        ai.Setup(s => s.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CONFIDENCE:LOW");
        ChatCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Student(tenantId, studentId, "en")],
            [],
            [],
            [],
            ai);

        ZimBotChatDto dto = await handler.Handle(
            new ChatCommand(tenantId, studentId, "Explain quantum foam", null, false),
            CancellationToken.None);

        Assert.True(dto.IsLowConfidence);
        Assert.Equal(ZimBotMessages.AskTeacher, dto.Reply);
    }

    [Fact]
    public async Task Teacher_cannot_chat_as_a_student()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ChatCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Teacher,
            Guid.NewGuid(),
            [Student(tenantId, studentId, "en")],
            [],
            [],
            [],
            new Mock<IAiService>());

        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(
                new ChatCommand(tenantId, studentId, "Hello", null, false),
                CancellationToken.None));
    }

    [Fact]
    public async Task Missing_student_throws_not_found()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        ChatCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [],
            [],
            [],
            [],
            new Mock<IAiService>());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new ChatCommand(tenantId, studentId, "Hello", null, false),
                CancellationToken.None));
    }

    [Fact]
    public async Task Resolves_current_module_from_incomplete_progress()
    {
        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        string? prompt = null;
        Mock<IAiService> ai = new();
        ai.Setup(s => s.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((system, _, _) => prompt = system)
            .ReturnsAsync("CONFIDENCE:HIGH\nKeep going.");
        ChatCommandHandler handler = CreateHandler(
            tenantId,
            UserRole.Student,
            studentId,
            [Student(tenantId, studentId, "en")],
            [Module(tenantId, moduleId, "Verbs", "English", GradeLevel.Grade2)],
            [Progress(tenantId, studentId, moduleId, isCompleted: false)],
            [],
            ai);

        await handler.Handle(
            new ChatCommand(tenantId, studentId, "Help with verbs", null, false),
            CancellationToken.None);

        Assert.NotNull(prompt);
        Assert.Contains("Verbs", prompt, StringComparison.Ordinal);
        Assert.Contains("Grade 2", prompt, StringComparison.Ordinal);
    }
}
