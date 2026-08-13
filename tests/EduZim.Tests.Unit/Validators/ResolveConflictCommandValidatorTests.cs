using EduZim.Application.Sync.Commands.ResolveConflict;
using EduZim.Application.Sync.DTOs;
using EduZim.Application.Sync.Services;

namespace EduZim.Tests.Unit.Validators;

public sealed class ResolveConflictCommandValidatorTests
{
    private readonly ResolveConflictCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        ResolveConflictCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            new OfflineSyncPayloadDto(
                OfflineSyncKinds.ModuleProgress,
                ModuleId: Guid.NewGuid(),
                TimeOnTaskSeconds: 20));

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Empty_queue_item_fails()
    {
        ResolveConflictCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            DateTime.UtcNow,
            new OfflineSyncPayloadDto(
                OfflineSyncKinds.ModuleProgress,
                ModuleId: Guid.NewGuid()));

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Default_timestamp_fails()
    {
        ResolveConflictCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            default,
            new OfflineSyncPayloadDto(
                OfflineSyncKinds.ModuleProgress,
                ModuleId: Guid.NewGuid()));

        FluentValidation.Results.ValidationResult result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }
}
