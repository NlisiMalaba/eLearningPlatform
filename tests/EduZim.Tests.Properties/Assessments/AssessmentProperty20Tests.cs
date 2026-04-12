using EduZim.Application.Assessments.Commands.AssignAssessment;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck;
using FsCheck.Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Assessments;

/// <summary>Feature: elearning-app-zimbabwe — Assessment property 20.</summary>
public sealed class AssessmentProperty20Tests
{
    // Feature: elearning-app-zimbabwe, Property 20: Assessment Assignment Notifies All Enrolled Students — Validates: Requirements 7.6
    [Property(MaxTest = 100)]
    public async Task Property20_assigning_assessment_creates_one_assessment_due_notification_per_enrolled_student(
        NonNegativeInt enrollmentCountSeed)
    {
        int n = Math.Min(enrollmentCountSeed.Get, 28);

        using ServiceProvider provider = AssessmentPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        MutableCurrentUser current = provider.GetRequiredService<MutableCurrentUser>();

        DateTime now = DateTime.UtcNow;
        Guid tenantId = Guid.NewGuid();
        Guid moduleId = Guid.NewGuid();
        Guid assessmentId = Guid.NewGuid();
        Guid classId = Guid.NewGuid();
        Guid teacherId = Guid.NewGuid();

        await db.Tenants.AddAsync(
                new Tenant
                {
                    Id = tenantId,
                    Name = "T",
                    Tier = TenantTier.School,
                    Status = TenantStatus.Active,
                    Branding = new BrandingSettings { SchoolName = "S", PrimaryColour = "#1976D2" },
                    CreatedAt = now,
                })
            .ConfigureAwait(false);

        await db.Modules.AddAsync(
                new Module
                {
                    Id = moduleId,
                    TenantId = tenantId,
                    Title = "Mod",
                    Grade = GradeLevel.Grade1,
                    Subject = "Subj",
                    SequenceOrder = 1,
                    IsRequired = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
            .ConfigureAwait(false);

        await db.Assessments.AddAsync(
                new Assessment
                {
                    Id = assessmentId,
                    TenantId = tenantId,
                    ModuleId = moduleId,
                    Title = "Exam",
                    TimeLimitSeconds = null,
                    PassingScorePercent = 60,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
            .ConfigureAwait(false);

        await db.SchoolClasses.AddAsync(
                new SchoolClass
                {
                    Id = classId,
                    TenantId = tenantId,
                    Name = "Class A",
                    CreatedAt = now,
                    UpdatedAt = now,
                })
            .ConfigureAwait(false);

        var studentIds = new List<Guid>();
        for (int i = 0; i < n; i++)
        {
            Guid sid = Guid.NewGuid();
            studentIds.Add(sid);
            await db.ClassEnrollments.AddAsync(
                    new ClassEnrollment
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        SchoolClassId = classId,
                        StudentUserId = sid,
                        CreatedAt = now,
                        UpdatedAt = now,
                    })
                .ConfigureAwait(false);
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        current.UserId = teacherId;
        current.TenantId = tenantId;
        current.Role = UserRole.Teacher;

        await mediator
            .Send(
                new AssignAssessmentCommand(tenantId, assessmentId, classId, DateTime.UtcNow.AddDays(7)),
                CancellationToken.None)
            .ConfigureAwait(false);

        List<Notification> notifications = await db.Notifications
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Type == NotificationType.AssessmentDue)
            .ToListAsync(CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(n, notifications.Count);
        Assert.Equal(studentIds.OrderBy(x => x).ToList(), notifications.Select(x => x.UserId).OrderBy(x => x).ToList());
    }
}
