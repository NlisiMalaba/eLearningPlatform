using EduZim.Application.Content;
using EduZim.Application.Content.Commands.ArchiveContent;
using EduZim.Application.Content.Commands.UploadContent;
using EduZim.Application.Exceptions;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Properties.Tenants;
using FsCheck;
using FsCheck.Xunit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Content;

/// <summary>Feature: elearning-app-zimbabwe — Content properties 10, 11, 18, 19, 38, 39.</summary>
public sealed class ContentPropertyTests
{
    // Property 10: Pre-school Video Duration Limit — Validates: Requirements 3.3
    [Property(MaxTest = 200)]
    public void Property10_pre_school_video_duration_must_not_exceed_five_minutes(
        NonNegativeInt durationSeconds,
        bool tenantIsPreSchool,
        int contentTypeOrdinal)
    {
        var tier = tenantIsPreSchool ? TenantTier.PreSchool : TenantTier.School;
        var types = Enum.GetValues<ContentType>();
        var type = types[Math.Abs(contentTypeOrdinal) % types.Length];
        var d = durationSeconds.Get;

        var compliant = ContentCatalogRules.IsPreSchoolVideoDurationCompliant(tier, type, d);
        var expected = tier != TenantTier.PreSchool || type != ContentType.Video || d <= ContentCatalogRules.PreSchoolMaxVideoDurationSeconds;
        Assert.Equal(expected, compliant);
    }

    // Property 11: At Least One Game Per Foundational Concept — Validates: Requirements 3.5
    [Property(MaxTest = 100)]
    public void Property11_each_category_with_items_has_at_least_one_game(NonEmptyArray<NonNegativeInt> categorySeeds)
    {
        var rnd = new Random(categorySeeds.Get.Aggregate(0, (a, x) => a ^ x.Get));
        var categories = categorySeeds.Get
            .Select((x, i) => $"cat-{x.Get}-{i}")
            .Distinct()
            .Take(6)
            .ToList();
        if (categories.Count == 0)
            categories.Add("cat-default");

        var types = Enum.GetValues<ContentType>();
        var items = new List<(string Category, ContentType Type)>();
        foreach (var cat in categories)
        {
            var n = rnd.Next(1, 4);
            for (var j = 0; j < n; j++)
                items.Add((cat, types[rnd.Next(types.Length)]));
        }

        var fromRules = ContentCatalogRules.EachFoundationalConceptCategoryHasAtLeastOneGame(items);
        var alternate = AlternateEachCategoryHasGame(items);
        Assert.Equal(alternate, fromRules);
    }

    private static bool AlternateEachCategoryHasGame(IReadOnlyList<(string Category, ContentType Type)> items)
    {
        foreach (var cat in items.Select(i => i.Category).Distinct())
        {
            var hasGame = false;
            foreach (var x in items)
            {
                if (x.Category == cat && x.Type == ContentType.Game)
                {
                    hasGame = true;
                    break;
                }
            }

            if (!hasGame)
                return false;
        }

        return true;
    }

    // Property 18: File Upload Size Enforcement — Validates: Requirements 7.1
    [Property(MaxTest = 50)]
    public async Task Property18_rejects_oversized_video_and_audio_before_storage()
    {
        using var provider = ContentPropertyTestHost.Create();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var storage = scope.ServiceProvider.GetRequiredService<FakeStorageService>();

        var tenantId = Guid.NewGuid();
        var user = provider.GetRequiredService<MutableCurrentUser>();
        user.UserId = Guid.NewGuid();
        user.TenantId = tenantId;
        user.Role = UserRole.Teacher;

        var videoOver = 500L * 1024 * 1024 + 1;
        await Assert.ThrowsAsync<ValidationException>(() =>
                mediator.Send(
                    new UploadContentCommand(
                        tenantId,
                        "v",
                        ContentType.Video,
                        "en",
                        videoOver,
                        new MemoryStream(),
                        "video/mp4"),
                    CancellationToken.None))
            .ConfigureAwait(false);
        Assert.Empty(storage.UploadedKeys);

        storage.UploadedKeys.Clear();
        var audioOver = 50L * 1024 * 1024 + 1;
        await Assert.ThrowsAsync<ValidationException>(() =>
                mediator.Send(
                    new UploadContentCommand(
                        tenantId,
                        "a",
                        ContentType.Audio,
                        "en",
                        audioOver,
                        new MemoryStream(),
                        "audio/mpeg"),
                    CancellationToken.None))
            .ConfigureAwait(false);
        Assert.Empty(storage.UploadedKeys);

        storage.UploadedKeys.Clear();
        var okVideo = 1024L;
        await mediator.Send(
                new UploadContentCommand(
                    tenantId,
                    "okv",
                    ContentType.Video,
                    "en",
                    okVideo,
                    new MemoryStream(new byte[1024]),
                    "video/mp4"),
                CancellationToken.None)
            .ConfigureAwait(false);
        Assert.NotEmpty(storage.UploadedKeys);
    }

    // Property 19: Soft Delete Retention — Validates: Requirements 7.4
    [Property(MaxTest = 50)]
    public async Task Property19_archive_sets_archived_and_schedules_permanent_deletion_after_retention_window()
    {
        var jobs = new RecordingContentBackgroundJobs();
        using var provider = ContentPropertyTestHost.Create(jobs);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var tenantId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = provider.GetRequiredService<MutableCurrentUser>();
        user.UserId = userId;
        user.TenantId = tenantId;
        user.Role = UserRole.Teacher;

        await db.ContentItems.AddAsync(
                new ContentItem
                {
                    Id = contentId,
                    TenantId = tenantId,
                    Title = "c",
                    Type = ContentType.Pdf,
                    StorageKey = $"{tenantId}/content/{contentId}/c",
                    FileSizeBytes = 10,
                    Language = "en",
                    Status = ContentStatus.Published,
                    UploadedByUserId = userId,
                    CreatedAt = now,
                    UpdatedAt = now,
                },
                CancellationToken.None)
            .ConfigureAwait(false);
        await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        await mediator.Send(new ArchiveContentCommand(tenantId, contentId), CancellationToken.None).ConfigureAwait(false);

        var row = await db.ContentItems.AsNoTracking()
            .SingleAsync(c => c.Id == contentId)
            .ConfigureAwait(false);
        Assert.Equal(ContentStatus.Archived, row.Status);
        Assert.NotNull(row.ArchivedAt);

        var scheduled = Assert.Single(jobs.Scheduled);
        Assert.Equal(tenantId, scheduled.TenantId);
        Assert.Equal(contentId, scheduled.ContentItemId);
        var expectedEarliest = row.ArchivedAt!.Value.AddDays(30);
        Assert.True(
            (scheduled.RunAtUtc - expectedEarliest).Duration() < TimeSpan.FromSeconds(2),
            $"Scheduled {scheduled.RunAtUtc:o} should match archived + 30 days ({expectedEarliest:o}).");
    }

    // Property 38: Closed Captions Required for Video Content — Validates: Requirements 14.4
    [Property(MaxTest = 200)]
    public void Property38_video_or_animation_with_audio_requires_at_least_one_caption_track(
        int typeOrdinal,
        bool hasAudio,
        NonNegativeInt captionCount)
    {
        var types = Enum.GetValues<ContentType>();
        var type = types[Math.Abs(typeOrdinal) % types.Length];
        var n = captionCount.Get;

        var satisfied = AccessibilityRules.CaptionRequirementSatisfied(type, hasAudio, n);
        var requires = AccessibilityRules.RequiresCaptionTracks(type, hasAudio);
        var expected = !requires || n >= 1;
        Assert.Equal(expected, satisfied);
    }

    // Property 39: Audio Content Transcript Required — Validates: Requirements 14.6
    [Property(MaxTest = 200)]
    public void Property39_audio_requires_transcript_record(int typeOrdinal, bool hasTranscript)
    {
        var types = Enum.GetValues<ContentType>();
        var type = types[Math.Abs(typeOrdinal) % types.Length];

        var satisfied = AccessibilityRules.TranscriptRequirementSatisfied(type, hasTranscript);
        var requires = AccessibilityRules.RequiresTranscript(type);
        var expected = !requires || hasTranscript;
        Assert.Equal(expected, satisfied);
    }
}
