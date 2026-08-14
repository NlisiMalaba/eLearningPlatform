namespace EduZim.Application.LiveClassrooms.Services;

public static class ClassroomReminderMessage
{
    public static string Build(string className, DateTime startAtUtc) =>
        $"A live classroom session for class \"{className}\" starts at {startAtUtc:yyyy-MM-dd HH:mm} UTC.";
}
