namespace EduZim.Application.ZimBot.Services;

public static class ZimBotMessages
{
    public const string Unavailable =
        "ZimBot is temporarily unavailable. Please try again shortly. In the meantime, open the help resources in your current module or ask your teacher.";

    public const string AskTeacher =
        "I'm not confident enough to help with this. Please ask your teacher, or use the help resources in your module.";

    public static bool IsUnavailable(string reply) =>
        string.Equals(reply.Trim(), Unavailable, StringComparison.Ordinal);
}
