using EduZim.Application.AdaptiveLearning.Services;

namespace EduZim.Tests.Unit.Services;

public sealed class AdaptiveDifficultyTests
{
    [Theory]
    [InlineData(5, 69, 4)]
    [InlineData(5, 50, 3)]
    [InlineData(3, 100, 3)]
    [InlineData(2, 69, 1)]
    public void ComputeAdjustedTier_reduces_tier_when_below_seventy(int baseline, int score, int expected)
    {
        int actual = AdaptiveDifficulty.ComputeAdjustedTier(baseline, score);
        Assert.Equal(expected, actual);
    }
}
