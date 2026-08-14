using EduZim.Application.Progress.Services;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ProgressRulesTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 4, 0)]
    [InlineData(1, 2, 50)]
    [InlineData(1, 3, 33)]
    [InlineData(2, 3, 67)]
    [InlineData(3, 3, 100)]
    public void Progress_percent_rounds_to_nearest_integer(int completed, int total, int expected)
    {
        Assert.Equal(expected, ProgressPercentageRules.Calculate(completed, total));
    }

    [Fact]
    public void Completing_module_makes_next_in_sequence_accessible()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        List<ModuleUnlockRules.ModuleSequenceRow> rows =
        [
            new(first, "Math", GradeLevel.Grade1, 1),
            new(second, "Math", GradeLevel.Grade1, 2),
        ];

        Assert.Equal(second, ModuleUnlockRules.NextModuleId(rows, first));
        Assert.True(ModuleUnlockRules.IsAccessible(rows, new HashSet<Guid> { first }, new HashSet<Guid>(), second));
    }

    [Fact]
    public void First_module_is_accessible_without_unlock_record()
    {
        Guid first = Guid.NewGuid();
        List<ModuleUnlockRules.ModuleSequenceRow> rows =
        [
            new(first, "Math", GradeLevel.Grade1, 1),
            new(Guid.NewGuid(), "Math", GradeLevel.Grade1, 2),
        ];

        Assert.True(ModuleUnlockRules.IsAccessible(rows, new HashSet<Guid>(), new HashSet<Guid>(), first));
    }

    [Fact]
    public void Current_grade_is_lowest_incomplete_grade()
    {
        Guid g1 = Guid.NewGuid();
        Guid g2 = Guid.NewGuid();
        List<ParentDashboardRules.ModuleGradeRow> rows =
        [
            new(g1, GradeLevel.Grade1, "Math"),
            new(g2, GradeLevel.Grade2, "Math"),
        ];

        GradeLevel grade = ParentDashboardRules.ResolveCurrentGrade(rows, new HashSet<Guid> { g1 });
        Assert.Equal(GradeLevel.Grade2, grade);
        Assert.Equal(["Math"], ParentDashboardRules.SubjectsForGrade(rows, grade));
    }

    [Fact]
    public void Weekly_summary_detects_already_sent_this_week()
    {
        DateTime weekStart = WeeklyProgressSummaryRules.WeekStartUtc(DateTime.UtcNow);
        Assert.True(WeeklyProgressSummaryRules.AlreadySentThisWeek([weekStart.AddHours(1)], weekStart));
        Assert.False(WeeklyProgressSummaryRules.AlreadySentThisWeek([weekStart.AddDays(-1)], weekStart));
    }
}
