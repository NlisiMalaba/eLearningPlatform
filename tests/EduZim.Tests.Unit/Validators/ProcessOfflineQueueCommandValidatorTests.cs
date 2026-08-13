using EduZim.Application.Sync.Commands.ProcessOfflineQueue;
using EduZim.Application.Sync.DTOs;
using EduZim.Application.Sync.Services;

namespace EduZim.Tests.Unit.Validators;

public sealed class ProcessOfflineQueueCommandValidatorTests
{
    private readonly ProcessOfflineQueueCommandValidator _validator = new();

    [Fact]
    public async Task Valid_module_progress_item_passes()
    {
        DateTime localTs = DateTime.UtcNow;
        ProcessOfflineQueueCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new OfflineSyncItemDto(
                    Guid.NewGuid(),
                    localTs,
                    new OfflineSyncPayloadDto(
                        OfflineSyncKinds.ModuleProgress,
                        ModuleId: Guid.NewGuid(),
                        IsCompleted: true,
                        CompletedAt: localTs,
                        TimeOnTaskSeconds: 30)),
            ]);

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_items_passes()
    {
        ProcessOfflineQueueCommand command = new(Guid.NewGuid(), Guid.NewGuid(), []);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_student_fails()
    {
        ProcessOfflineQueueCommand command = new(Guid.NewGuid(), Guid.Empty, []);
        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Unknown_kind_fails()
    {
        ProcessOfflineQueueCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OfflineSyncItemDto(null, DateTime.UtcNow, new OfflineSyncPayloadDto("Unknown"))]);

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Module_progress_without_module_id_fails()
    {
        ProcessOfflineQueueCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new OfflineSyncItemDto(
                    null,
                    DateTime.UtcNow,
                    new OfflineSyncPayloadDto(OfflineSyncKinds.ModuleProgress)),
            ]);

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Assessment_attempt_without_score_fails()
    {
        ProcessOfflineQueueCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new OfflineSyncItemDto(
                    null,
                    DateTime.UtcNow,
                    new OfflineSyncPayloadDto(
                        OfflineSyncKinds.AssessmentAttempt,
                        AssessmentId: Guid.NewGuid(),
                        TimeTakenSeconds: 10,
                        SubmittedAt: DateTime.UtcNow)),
            ]);

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Valid_assessment_attempt_passes()
    {
        ProcessOfflineQueueCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new OfflineSyncItemDto(
                    null,
                    DateTime.UtcNow,
                    new OfflineSyncPayloadDto(
                        OfflineSyncKinds.AssessmentAttempt,
                        AssessmentId: Guid.NewGuid(),
                        ScorePercent: 75,
                        TimeTakenSeconds: 120,
                        SubmittedAt: DateTime.UtcNow)),
            ]);

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }
}
