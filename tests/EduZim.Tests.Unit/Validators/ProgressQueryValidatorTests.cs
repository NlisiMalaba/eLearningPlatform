using EduZim.Application.Progress.Queries.GetParentDashboard;
using EduZim.Application.Progress.Queries.GetStudentProgress;
using EduZim.Application.Progress.Commands.SendParentWeeklySummaries;

namespace EduZim.Tests.Unit.Validators;

public sealed class ProgressQueryValidatorTests
{
    [Fact]
    public async Task Student_progress_valid_query_passes()
    {
        var validator = new GetStudentProgressQueryValidator();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new GetStudentProgressQuery(Guid.NewGuid(), Guid.NewGuid()));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Student_progress_empty_student_fails()
    {
        var validator = new GetStudentProgressQueryValidator();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new GetStudentProgressQuery(Guid.NewGuid(), Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Parent_dashboard_valid_query_passes()
    {
        var validator = new GetParentDashboardQueryValidator();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new GetParentDashboardQuery(Guid.NewGuid(), Guid.NewGuid()));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Parent_dashboard_empty_parent_fails()
    {
        var validator = new GetParentDashboardQueryValidator();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new GetParentDashboardQuery(Guid.NewGuid(), Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Weekly_summary_command_passes()
    {
        var validator = new SendParentWeeklySummariesCommandValidator();
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new SendParentWeeklySummariesCommand());
        Assert.True(result.IsValid);
    }
}
