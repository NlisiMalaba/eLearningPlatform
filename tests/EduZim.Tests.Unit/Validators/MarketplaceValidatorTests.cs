using EduZim.Application.Marketplace.Commands.ApproveAccess;
using EduZim.Application.Marketplace.Commands.ApproveContentPack;
using EduZim.Application.Marketplace.Commands.RateContentPack;
using EduZim.Application.Marketplace.Commands.RemoveContentPack;
using EduZim.Application.Marketplace.Commands.RequestAccess;
using EduZim.Application.Marketplace.Commands.SubmitContentPack;
using EduZim.Application.Marketplace.Queries.BrowseContentPacks;
using EduZim.Application.Marketplace.Queries.GetContentPack;

namespace EduZim.Tests.Unit.Validators;

public sealed class SubmitContentPackCommandValidatorTests
{
    private readonly SubmitContentPackCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes()
    {
        SubmitContentPackCommand command = new(
            Guid.NewGuid(),
            "Grade 4 Fractions",
            "Shared lessons",
            [Guid.NewGuid()]);
        Assert.True((await _validator.ValidateAsync(command)).IsValid);
    }

    [Fact]
    public async Task Empty_title_or_items_fails()
    {
        SubmitContentPackCommand emptyTitle = new(Guid.NewGuid(), "", "Desc", [Guid.NewGuid()]);
        SubmitContentPackCommand emptyItems = new(Guid.NewGuid(), "Title", "Desc", []);
        Assert.False((await _validator.ValidateAsync(emptyTitle)).IsValid);
        Assert.False((await _validator.ValidateAsync(emptyItems)).IsValid);
    }
}

public sealed class ApproveContentPackCommandValidatorTests
{
    private readonly ApproveContentPackCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes() =>
        Assert.True((await _validator.ValidateAsync(new ApproveContentPackCommand(Guid.NewGuid()))).IsValid);

    [Fact]
    public async Task Empty_id_fails() =>
        Assert.False((await _validator.ValidateAsync(new ApproveContentPackCommand(Guid.Empty))).IsValid);
}

public sealed class RequestAccessCommandValidatorTests
{
    private readonly RequestAccessCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes() =>
        Assert.True(
            (await _validator.ValidateAsync(new RequestAccessCommand(Guid.NewGuid(), Guid.NewGuid()))).IsValid);

    [Fact]
    public async Task Empty_pack_fails() =>
        Assert.False(
            (await _validator.ValidateAsync(new RequestAccessCommand(Guid.NewGuid(), Guid.Empty))).IsValid);
}

public sealed class ApproveAccessCommandValidatorTests
{
    private readonly ApproveAccessCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes() =>
        Assert.True(
            (await _validator.ValidateAsync(
                new ApproveAccessCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()))).IsValid);

    [Fact]
    public async Task Same_tenant_fails()
    {
        Guid tenantId = Guid.NewGuid();
        Assert.False(
            (await _validator.ValidateAsync(new ApproveAccessCommand(tenantId, Guid.NewGuid(), tenantId))).IsValid);
    }
}

public sealed class RateContentPackCommandValidatorTests
{
    private readonly RateContentPackCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes() =>
        Assert.True(
            (await _validator.ValidateAsync(
                new RateContentPackCommand(Guid.NewGuid(), Guid.NewGuid(), 4, "Good"))).IsValid);

    [Fact]
    public async Task Rating_out_of_range_fails()
    {
        RateContentPackCommand tooLow = new(Guid.NewGuid(), Guid.NewGuid(), 0, null);
        RateContentPackCommand tooHigh = new(Guid.NewGuid(), Guid.NewGuid(), 6, null);
        Assert.False((await _validator.ValidateAsync(tooLow)).IsValid);
        Assert.False((await _validator.ValidateAsync(tooHigh)).IsValid);
    }
}

public sealed class RemoveContentPackCommandValidatorTests
{
    private readonly RemoveContentPackCommandValidator _validator = new();

    [Fact]
    public async Task Valid_command_passes() =>
        Assert.True((await _validator.ValidateAsync(new RemoveContentPackCommand(Guid.NewGuid()))).IsValid);

    [Fact]
    public async Task Empty_id_fails() =>
        Assert.False((await _validator.ValidateAsync(new RemoveContentPackCommand(Guid.Empty))).IsValid);
}

public sealed class BrowseContentPacksQueryValidatorTests
{
    private readonly BrowseContentPacksQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes() =>
        Assert.True((await _validator.ValidateAsync(new BrowseContentPacksQuery(Guid.NewGuid()))).IsValid);

    [Fact]
    public async Task Empty_tenant_fails() =>
        Assert.False((await _validator.ValidateAsync(new BrowseContentPacksQuery(Guid.Empty))).IsValid);
}

public sealed class GetContentPackQueryValidatorTests
{
    private readonly GetContentPackQueryValidator _validator = new();

    [Fact]
    public async Task Valid_query_passes() =>
        Assert.True(
            (await _validator.ValidateAsync(new GetContentPackQuery(Guid.NewGuid(), Guid.NewGuid()))).IsValid);

    [Fact]
    public async Task Empty_pack_fails() =>
        Assert.False(
            (await _validator.ValidateAsync(new GetContentPackQuery(Guid.NewGuid(), Guid.Empty))).IsValid);
}
