using EduZim.Application.Assessments.Commands.AssignAssessment;
using EduZim.Application.Assessments.Commands.AutoSubmitAssessment;
using EduZim.Application.Assessments.Commands.BeginAssessmentSession;
using EduZim.Application.Assessments.Commands.CreateAssessment;
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

/// <summary>Feature: elearning-app-zimbabwe — Assessment property 28 (auto-submit path).</summary>
public sealed class AssessmentProperty28AutoSubmitTests
{
    // Feature: elearning-app-zimbabwe, Property 28: Timed Assessment Auto-Submit — Validates: Requirements 9.8
    [Property(MaxTest = 100)]
    public async Task Property28_auto_submit_records_time_taken_capped_at_assessment_time_limit()
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
        const int limitSeconds = 180;

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
                    "Timed auto",
                    TimeLimitSeconds: limitSeconds,
                    PassingScorePercent: 60,
                    new List<CreateAssessmentQuestionItem>
                    {
                        new(
                            QuestionType.ShortAnswer,
                            "y",
                            1,
                            OptionTexts: null,
                            CorrectOptionIndex: null,
                            CorrectShortAnswer: "b"),
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
        current.Role = UserRole.Student;
        current.TenantId = tenantId;

        BeginAssessmentSessionResultDto session = await mediator
            .Send(
                new BeginAssessmentSessionCommand(tenantId, assessmentId, classId),
                CancellationToken.None)
            .ConfigureAwait(false);

        AssessmentAttempt attempt = await db.AssessmentAttempts
            .FirstAsync(a => a.Id == session.AttemptId, CancellationToken.None)
            .ConfigureAwait(false);
        attempt.StartedAt = DateTime.UtcNow.AddSeconds(-(limitSeconds + 400));
        await db.SaveChangesAsync().ConfigureAwait(false);

        await mediator
            .Send(new AutoSubmitAssessmentCommand(tenantId, session.AttemptId), CancellationToken.None)
            .ConfigureAwait(false);

        AssessmentAttempt completed = await db.AssessmentAttempts.AsNoTracking()
            .FirstAsync(a => a.Id == session.AttemptId, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(completed.SubmittedAt);
        Assert.Equal(limitSeconds, completed.TimeTakenSeconds);
        Assert.InRange(completed.ScorePercent, 0, 100);
    }
}
