namespace EduZim.Application.Assessments.Notifications;

internal static class AssessmentSubmittedParentMessage
{
    public static string Build(string studentDisplayName, string assessmentTitle, int scorePercent) =>
        $"{studentDisplayName} scored {scorePercent}% on \"{assessmentTitle}\".";
}
