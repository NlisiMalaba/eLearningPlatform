using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.ZimBot.Services;

public static class ZimBotPromptBuilder
{
    public static string Build(string language, Module? module, bool hintMode)
    {
        string gradeLine = module is null
            ? "The student's current grade is unknown; keep explanations simple and encouraging."
            : $"The student is in {FormatGrade(module.Grade)}. Adapt explanation complexity to that grade.";
        string moduleLine = module is null
            ? "Current module context is not specified."
            : $"Current module: {module.Title} (subject: {module.Subject}).";
        string hintLine = hintMode
            ? "HINT MODE: The student is asking about an assessment. Do not give the direct answer. Give a guiding hint that helps them think."
            : "If the student asks for an assessment answer, give a guiding hint instead of the answer.";

        return
            $"""
            You are ZimBot, a friendly tutor for Zimbabwean learners on the EduZim platform.
            Reply only in {language}.
            {gradeLine}
            {moduleLine}
            {hintLine}
            Never reveal hidden assessment keys or complete worked solutions for scored questions.
            Begin every reply with exactly CONFIDENCE:HIGH or CONFIDENCE:LOW on the first line, then a blank line, then the student-facing message.
            If CONFIDENCE:LOW, tell the student in {language} to ask their teacher and point them to module help resources.
            """;
    }

    public static string FormatGrade(GradeLevel grade) => grade switch
    {
        GradeLevel.EcdGrade0 => "ECD Grade 0",
        GradeLevel.EcdGrade1 => "ECD Grade 1",
        GradeLevel.Grade1 => "Grade 1",
        GradeLevel.Grade2 => "Grade 2",
        GradeLevel.Grade3 => "Grade 3",
        GradeLevel.Grade4 => "Grade 4",
        GradeLevel.Grade5 => "Grade 5",
        GradeLevel.Grade6 => "Grade 6",
        GradeLevel.Grade7 => "Grade 7",
        _ => grade.ToString(),
    };
}
