using EduZim.Application.Common.Interfaces;
using EduZim.Application.Assessments.Queries.ListAssessments;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ListAssessmentsQueryHandlerTests
{
    [Fact]
    public async Task Returns_assessments_with_question_counts_for_the_tenant()
    {
        Guid tenantId = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.Assessments).Returns(new List<Assessment>
        {
            new()
            {
                Id = assessmentId,
                TenantId = tenantId,
                Title = "Fractions quiz",
                ModuleId = Guid.NewGuid(),
                PassingScorePercent = 60,
                TimeLimitSeconds = 600,
            },
        }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Questions).Returns(new List<Question>
        {
            new() { Id = Guid.NewGuid(), TenantId = tenantId, AssessmentId = assessmentId, Text = "Q1" },
            new() { Id = Guid.NewGuid(), TenantId = tenantId, AssessmentId = assessmentId, Text = "Q2" },
        }.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Teacher);
        user.Setup(u => u.TenantId).Returns(tenantId);

        ListAssessmentsQueryHandler handler = new(db.Object, user.Object);
        IReadOnlyList<AssessmentListItemDto> items = await handler.Handle(
            new ListAssessmentsQuery(tenantId),
            CancellationToken.None);

        AssessmentListItemDto dto = Assert.Single(items);
        Assert.Equal("Fractions quiz", dto.Title);
        Assert.Equal(2, dto.QuestionCount);
        Assert.Equal(600, dto.TimeLimitSeconds);
    }

    [Fact]
    public async Task Student_cannot_list_assessments()
    {
        Guid tenantId = Guid.NewGuid();
        Mock<IEduZimDbContext> db = new();
        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.Student);
        user.Setup(u => u.TenantId).Returns(tenantId);

        ListAssessmentsQueryHandler handler = new(db.Object, user.Object);
        await Assert.ThrowsAsync<TenantAccessViolationException>(
            () => handler.Handle(new ListAssessmentsQuery(tenantId), CancellationToken.None));
    }
}
