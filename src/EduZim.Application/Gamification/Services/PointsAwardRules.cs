namespace EduZim.Application.Gamification.Services;

/// <summary>Point awards for module and assessment completion (correctness property 25).</summary>
public static class PointsAwardRules
{
    public const int ModuleCompletionBasePoints = 10;
    public const int AssessmentCompletionBasePoints = 10;
    public const int HighScoreThresholdPercent = 85;
    public const decimal HighScoreBonusMultiplier = 1.5m;

    public static int ForModuleCompletion() => ModuleCompletionBasePoints;

    public static int ForAssessment(int scorePercent)
    {
        if (scorePercent >= HighScoreThresholdPercent)
            return ApplyBonusMultiplier(AssessmentCompletionBasePoints);

        return AssessmentCompletionBasePoints;
    }

    public static int ApplyBonusMultiplier(int basePoints) =>
        (int)decimal.Round(basePoints * HighScoreBonusMultiplier, MidpointRounding.AwayFromZero);
}
