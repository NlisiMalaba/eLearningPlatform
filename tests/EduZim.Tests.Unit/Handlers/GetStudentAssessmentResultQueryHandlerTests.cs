using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Assessments.Queries.GetStudentAssessmentResult;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class GetStudentAssessmentResultQueryHandlerTests
{
    [Fact]
    public async Task Teacher_loads_submitted_attempt_result()
    {
        Guid tenantId = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid questionId = Guid.NewGuid();
        Guid attemptId = Guid.NewGuid();
        DateTime submitted = DateTime.UtcNow.AddMinutes(-1);

        var assessment = new Assessment
        {
            Id = assessmentId,
            TenantId = tenantId,
            ModuleId = Guid.NewGuid(),
            Title = "Test",
            PassingScorePercent = 60,
            CreatedAt = submitted,
            UpdatedAt = submitted,
        };

        var question = new Question
        {
            Id = questionId,
            TenantId = tenantId,
            AssessmentId = assessmentId,
            Type = QuestionType.ShortAnswer,
            Text = "Q",
            Points = 10,
            CorrectAnswer = "a",
        };

        var attempt = new AssessmentAttempt
        {
            Id = attemptId,
            TenantId = tenantId,
            AssessmentId = assessmentId,
            StudentId = studentId,
            ScorePercent = 100,
            TimeTakenSeconds = 30,
            SubmittedAt = submitted,
            StartedAt = submitted.AddMinutes(-5),
            CreatedAt = submitted,
            UpdatedAt = submitted,
        };

        var answer = new AnswerRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AssessmentAttemptId = attemptId,
            QuestionId = questionId,
            Answer = "a",
        };

        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        db.Setup(x => x.Assessments)
            .Returns(new List<Assessment> { assessment }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Questions)
            .Returns(new List<Question> { question }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.AssessmentAttempts)
            .Returns(new List<AssessmentAttempt> { attempt }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.AnswerRecords)
            .Returns(new List<AnswerRecord> { answer }.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Teacher);
        user.Setup(u => u.TenantId).Returns(tenantId);
        user.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var handler = new GetStudentAssessmentResultQueryHandler(
            db.Object,
            user.Object,
            NullLogger<GetStudentAssessmentResultQueryHandler>.Instance);

        SubmitAssessmentResultDto result = await handler.Handle(
            new GetStudentAssessmentResultQuery(tenantId, assessmentId, studentId),
            CancellationToken.None);

        Assert.Equal(attemptId, result.AttemptId);
        Assert.Equal(100, result.ScorePercent);
        Assert.Single(result.QuestionFeedback);
    }
}
