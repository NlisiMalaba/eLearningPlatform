namespace EduZim.Application.AdaptiveLearning.Services;

public static class AdaptiveDifficulty
{
    /// <summary>Lower tier when last score is under 70%, one tier per ~10 points deficit.</summary>
    public static int ComputeAdjustedTier(int baselineTier, int lastScorePercent)
    {
        if (lastScorePercent >= 70)
            return baselineTier;

        int deficit = 70 - lastScorePercent;
        int steps = (deficit + 9) / 10;
        return Math.Max(1, baselineTier - steps);
    }
}
