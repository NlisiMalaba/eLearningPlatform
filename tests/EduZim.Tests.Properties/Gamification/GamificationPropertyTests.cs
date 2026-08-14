using EduZim.Application.Gamification.DTOs;
using EduZim.Application.Gamification.Queries.GetLeaderboard;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck;
using FsCheck.Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Gamification;

/// <summary>Feature: elearning-app-zimbabwe — Gamification properties 25, 26, 27.</summary>
public sealed class GamificationPropertyTests
{
    private const int SpecBaseAward = 10;
    private const int SpecBonusThreshold = 85;
    private const decimal SpecBonusMultiplier = 1.5m;

    // Feature: elearning-app-zimbabwe, Property 25: Points Awarded on Module and Assessment Completion — Validates: Requirements 9.3
    [Property(MaxTest = 100)]
    public async Task Property25_completion_increases_total_points_with_bonus_at_or_above_85(
        NonNegativeInt startGen,
        NonNegativeInt scoreGen,
        bool moduleCompletion)
    {
        int starting = startGen.Get % 400;
        int score = scoreGen.Get % 101;
        using ServiceProvider provider = GamificationPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        await GamificationPropertyTestHost.SeedStartingPointsAsync(db, tenantId, studentId, starting)
            .ConfigureAwait(false);

        int expectedDelta = moduleCompletion ? OracleModuleAward() : OracleAssessmentAward(score);
        await PublishCompletionAsync(publisher, tenantId, studentId, moduleCompletion, score)
            .ConfigureAwait(false);

        int actual = await db.StudentPoints.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.StudentId == studentId)
            .Select(p => p.TotalPoints)
            .SingleAsync()
            .ConfigureAwait(false);

        Assert.Equal(starting + expectedDelta, actual);
        Assert.True(expectedDelta >= SpecBaseAward);
        if (!moduleCompletion && score >= SpecBonusThreshold)
            Assert.Equal(OracleBonus(SpecBaseAward), expectedDelta);
    }

    // Feature: elearning-app-zimbabwe, Property 26: Badge Awarded on Milestone Events — Validates: Requirements 9.4, 9.6
    [Property(MaxTest = 100)]
    public async Task Property26_milestone_creates_badge_and_queues_certificate(byte milestoneKindRaw)
    {
        BadgeType expected = (BadgeType)(milestoneKindRaw % 4);
        using ServiceProvider provider = GamificationPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        RecordingGamificationBackgroundJobs jobs =
            provider.GetRequiredService<RecordingGamificationBackgroundJobs>();

        Guid tenantId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        await GamificationPropertySeeds.SeedMilestoneAsync(db, publisher, tenantId, studentId, expected)
            .ConfigureAwait(false);

        List<Badge> badges = await db.Badges.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.StudentId == studentId)
            .ToListAsync()
            .ConfigureAwait(false);

        Assert.Contains(badges, b => b.Type == expected);
        Assert.Equal(badges.Count, jobs.Queued.Count);
        Assert.All(
            badges,
            b => Assert.Contains(jobs.Queued, q => q.BadgeId == b.Id && q.StudentId == studentId && q.TenantId == tenantId));
    }

    // Feature: elearning-app-zimbabwe, Property 27: Leaderboard Tenant Isolation — Validates: Requirements 9.5
    [Property(MaxTest = 100)]
    public async Task Property27_leaderboard_contains_only_requesting_tenant(
        byte homeCountRaw,
        byte otherCountRaw,
        byte limitRaw)
    {
        int homeCount = homeCountRaw % 12;
        int otherCount = otherCountRaw % 12;
        int limit = (limitRaw % 100) + 1;
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        using ServiceProvider provider = GamificationPropertyTestHost.Create();
        using IServiceScope scope = provider.CreateScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        MutableCurrentUser current = provider.GetRequiredService<MutableCurrentUser>();
        current.UserId = Guid.NewGuid();
        current.TenantId = tenantA;
        current.Role = UserRole.Teacher;

        HashSet<Guid> homeIds = await GamificationPropertySeeds
            .SeedMixedTenantPointsAsync(db, tenantA, tenantB, homeCount, otherCount)
            .ConfigureAwait(false);

        LeaderboardDto dto = await mediator
            .Send(new GetLeaderboardQuery(tenantA, limit), CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal(tenantA, dto.TenantId);
        Assert.All(dto.Entries, e => Assert.Equal(tenantA, e.TenantId));
        Assert.All(dto.Entries, e => Assert.Contains(e.StudentId, homeIds));
        Assert.True(dto.Entries.Count <= Math.Min(limit, homeCount));
        Assert.DoesNotContain(dto.Entries, e => e.TenantId == tenantB);
    }

    private static int OracleModuleAward() => SpecBaseAward;

    private static int OracleAssessmentAward(int scorePercent) =>
        scorePercent >= SpecBonusThreshold ? OracleBonus(SpecBaseAward) : SpecBaseAward;

    private static int OracleBonus(int basePoints) =>
        (int)decimal.Round(basePoints * SpecBonusMultiplier, MidpointRounding.AwayFromZero);

    private static Task PublishCompletionAsync(
        IPublisher publisher,
        Guid tenantId,
        Guid studentId,
        bool moduleCompletion,
        int score)
    {
        if (moduleCompletion)
        {
            return publisher.Publish(
                new ModuleCompletedNotification(studentId, Guid.NewGuid(), tenantId),
                CancellationToken.None);
        }

        return publisher.Publish(
            new AssessmentSubmittedNotification(
                studentId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                tenantId,
                score),
            CancellationToken.None);
    }
}
