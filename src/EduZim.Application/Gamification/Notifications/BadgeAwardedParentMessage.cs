using EduZim.Domain.Enums;

namespace EduZim.Application.Gamification.Notifications;

internal static class BadgeAwardedParentMessage
{
    public static string Build(string studentDisplayName, BadgeType badgeType) =>
        $"{studentDisplayName} earned the {DisplayName(badgeType)} badge.";

    public static string DisplayName(BadgeType badgeType) => badgeType switch
    {
        BadgeType.FirstModule => "first module",
        BadgeType.FiveConsecutiveDays => "5 consecutive days",
        BadgeType.SubjectMastery => "subject mastery",
        BadgeType.GradeCompletion => "grade completion",
        _ => badgeType.ToString(),
    };
}
