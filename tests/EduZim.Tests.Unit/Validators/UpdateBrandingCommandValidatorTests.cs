using EduZim.Application.Tenants.Commands.UpdateBranding;
using EduZim.Domain.Entities;

namespace EduZim.Tests.Unit.Validators;

public sealed class UpdateBrandingCommandValidatorTests
{
    [Fact]
    public async Task Rejects_non_hex_primary_colour()
    {
        UpdateBrandingCommandValidator validator = new();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new UpdateBrandingCommand(
                Guid.NewGuid(),
                new BrandingSettings { SchoolName = "School", PrimaryColour = "green" }));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Accepts_hex_primary_colour()
    {
        UpdateBrandingCommandValidator validator = new();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new UpdateBrandingCommand(
                Guid.NewGuid(),
                new BrandingSettings { SchoolName = "School", PrimaryColour = "#0B6E4F" }));
        Assert.True(result.IsValid);
    }
}
