using System.Text.RegularExpressions;

namespace EduZim.Application.ZimBot.Services;

public static class AssessmentHintDetector
{
    private static readonly string[] AnswerPhrases =
    [
        "what is the answer",
        "what's the answer",
        "whats the answer",
        "tell me the answer",
        "give me the answer",
        "give me the correct",
        "what is the correct answer",
        "what's the correct answer",
        "which option is correct",
        "which one is correct",
        "solve this for me",
        "just tell me",
        "the correct option",
    ];

    public static bool IsAnswerRequest(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        string normalized = Normalize(message);
        foreach (string phrase in AnswerPhrases)
        {
            if (normalized.Contains(phrase, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string Normalize(string message)
    {
        string lower = message.Trim().ToLowerInvariant();
        return Regex.Replace(lower, @"\s+", " ");
    }
}
