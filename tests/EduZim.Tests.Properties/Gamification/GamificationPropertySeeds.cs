using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using EduZim.Infrastructure.Persistence;
using MediatR;

namespace EduZim.Tests.Properties.Gamification;

internal static class GamificationPropertySeeds
{
    public static async Task SeedMilestoneAsync(
        EduZimDbContext db,
        IPublisher publisher,
        Guid tenantId,
        Guid studentId,
        BadgeType expected)
    {
        if (expected == BadgeType.FiveConsecutiveDays)
        {
            await SeedFiveDayStreakAsync(db, publisher, tenantId, studentId).ConfigureAwait(false);
            return;
        }

        await SeedModuleMilestoneAsync(db, publisher, tenantId, studentId, expected).ConfigureAwait(false);
    }

    public static async Task<HashSet<Guid>> SeedMixedTenantPointsAsync(
        EduZimDbContext db,
        Guid tenantA,
        Guid tenantB,
        int homeCount,
        int otherCount)
    {
        HashSet<Guid> homeIds = new();
        for (int i = 0; i < homeCount; i++)
        {
            Guid id = Guid.NewGuid();
            homeIds.Add(id);
            await GamificationPropertyTestHost.SeedPointsRowAsync(db, tenantA, id, i + 1).ConfigureAwait(false);
        }

        for (int i = 0; i < otherCount; i++)
        {
            await GamificationPropertyTestHost
                .SeedPointsRowAsync(db, tenantB, Guid.NewGuid(), 1000 + i)
                .ConfigureAwait(false);
        }

        return homeIds;
    }

    private static async Task SeedFiveDayStreakAsync(
        EduZimDbContext db,
        IPublisher publisher,
        Guid tenantId,
        Guid studentId)
    {
        DateTime today = DateTime.UtcNow.Date;
        for (int i = 1; i <= 4; i++)
        {
            await GamificationPropertyTestHost
                .SeedSubmittedAttemptAsync(db, tenantId, studentId, today.AddDays(-i))
                .ConfigureAwait(false);
        }

        await publisher.Publish(
                new AssessmentSubmittedNotification(
                    studentId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    tenantId,
                    70),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    private static async Task SeedModuleMilestoneAsync(
        EduZimDbContext db,
        IPublisher publisher,
        Guid tenantId,
        Guid studentId,
        BadgeType expected)
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        string subject = expected == BadgeType.SubjectMastery ? "Science" : "Math";
        GradeLevel grade = expected == BadgeType.GradeCompletion ? GradeLevel.Grade4 : GradeLevel.Grade1;
        await GamificationPropertyTestHost.SeedModuleAsync(db, tenantId, first, subject, grade)
            .ConfigureAwait(false);
        await GamificationPropertyTestHost.SeedModuleAsync(db, tenantId, second, subject, grade)
            .ConfigureAwait(false);

        if (expected is BadgeType.SubjectMastery or BadgeType.GradeCompletion)
        {
            await GamificationPropertyTestHost
                .SeedCompletedProgressAsync(db, tenantId, studentId, first, DateTime.UtcNow)
                .ConfigureAwait(false);
            await publisher.Publish(new ModuleCompletedNotification(studentId, second, tenantId), CancellationToken.None)
                .ConfigureAwait(false);
            return;
        }

        await publisher.Publish(new ModuleCompletedNotification(studentId, first, tenantId), CancellationToken.None)
            .ConfigureAwait(false);
    }
}
