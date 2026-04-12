using EduZim.Application.Assessments.Commands.AssignAssessment;
using EduZim.Application.Assessments.Commands.BeginAssessmentSession;
using EduZim.Application.Assessments.Commands.CreateAssessment;
using EduZim.Application.Assessments.Commands.SubmitAssessment;
using EduZim.Application.Assessments.DTOs;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck.Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Assessments;

/// <summary>Feature: elearning-app-zimbabwe — Assessment property 24.</summary>
public sealed class AssessmentProperty24Tests
{
    // Feature: elearning-app-zimbabwe, Property 24: Assessment Attempt Record Completeness — Validates: Requirements 9.1
    [Property(MaxTest = 100)]
    public async Task Property24_submitted_attempt_records_score_time_taken_and_submitted_timestamp()
    {
        using ServiceProvider provider = AssessmentPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        MutableCurrentUser current = provider.GetRequiredService<MutableCurrentUser>();

        DateTime now = DateTime.UtcNow;
        Guid tenantId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();

        await AssessmentPropertyTestHost.SeedTenantModuleClassAsync(db, tenantId, moduleId, classId, now)
            .ConfigureAwait(false);

        current.UserId = teacherId;
        current.TenantId = tenantId;
        current.Role = UserRole.Teacher;

        Guid assessmentId = await mediator
            .Send(
                new CreateAssessmentCommand(
                    tenantId,
                    moduleId,
                    "Quiz",
                    TimeLimitSeconds: null,
                    PassingScorePercent: 60,
                    new List<CreateAssessmentQuestionItem>
                    {
                        new(
                            QuestionType.ShortAnswer,
                            "Name?",
                            10,
                            OptionTexts: null,
                            CorrectOptionIndex: null,
                            CorrectShortAnswer: "EduZim"),
                    }),
                CancellationToken.None)
            .ConfigureAwait(false);

        await db.ClassEnrollments.AddAsync(
                new ClassEnrollment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SchoolClassId = classId,
                    StudentUserId = studentId,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
            .ConfigureAwait(false);
        await db.SaveChangesAsync().ConfigureAwait(false);

        await mediator
            .Send(
                new AssignAssessmentCommand(tenantId, assessmentId, classId, DateTime.UtcNow.AddDays(7)),
                CancellationToken.None)
            .ConfigureAwait(false);

        current.UserId = studentId;
        current.TenantId = tenantId;
        current.Role = UserRole.Student;

        BeginAssessmentSessionResultDto session = await mediator
            .Send(
                new BeginAssessmentSessionCommand(tenantId, assessmentId, classId),
                CancellationToken.None)
            .ConfigureAwait(false);

        Question question = await db.Questions.AsNoTracking()
            .FirstAsync(q => q.AssessmentId == assessmentId, CancellationToken.None)
            .ConfigureAwait(false);

        await mediator
            .Send(
                new SubmitAssessmentCommand(
                    tenantId,
                    assessmentId,
                    session.AttemptId,
                    new List<SubmitAssessmentAnswerItem>
                    {
                        new(question.Id, "EduZim"),
                    }),
                CancellationToken.None)
            .ConfigureAwait(false);

        AssessmentAttempt attempt = await db.AssessmentAttempts.AsNoTracking()
            .FirstAsync(a => a.Id == session.AttemptId, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(attempt.SubmittedAt);
        Assert.True(attempt.TimeTakenSeconds >= 0);
        Assert.InRange(attempt.ScorePercent, 0, 100);
    }
}
