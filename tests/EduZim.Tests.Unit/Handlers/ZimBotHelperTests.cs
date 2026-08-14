using EduZim.Application.ZimBot.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Tests.Unit.Handlers;

public sealed class ZimBotHelperTests
{
    [Theory]
    [InlineData("What is the answer to question 3?", true)]
    [InlineData("Please give me the correct option", true)]
    [InlineData("How does evaporation work?", false)]
    public void Hint_detector_flags_answer_requests(string message, bool expected)
    {
        Assert.Equal(expected, AssessmentHintDetector.IsAnswerRequest(message));
    }

    [Theory]
    [InlineData(null, ZimBotLanguage.English)]
    [InlineData("sn", ZimBotLanguage.Shona)]
    [InlineData("Ndebele", ZimBotLanguage.Ndebele)]
    [InlineData("kalanga", ZimBotLanguage.Kalanga)]
    public void Language_resolve_maps_known_codes(string? preferred, string expected)
    {
        Assert.Equal(expected, ZimBotLanguage.Resolve(preferred));
    }

    [Fact]
    public void Prompt_includes_grade_module_language_and_hint_mode()
    {
        Module module = new()
        {
            Title = "Fractions",
            Subject = "Mathematics",
            Grade = GradeLevel.Grade4,
        };

        string prompt = ZimBotPromptBuilder.Build(ZimBotLanguage.Shona, module, hintMode: true);

        Assert.Contains("Shona", prompt, StringComparison.Ordinal);
        Assert.Contains("Grade 4", prompt, StringComparison.Ordinal);
        Assert.Contains("Fractions", prompt, StringComparison.Ordinal);
        Assert.Contains("HINT MODE", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_strips_confidence_prefix()
    {
        (bool low, string body) = ZimBotResponseParser.Parse("CONFIDENCE:LOW\nAsk your teacher about fractions.");
        Assert.True(low);
        Assert.Equal("Ask your teacher about fractions.", body);
    }

    [Fact]
    public void Composer_uses_static_fallback_message()
    {
        (string reply, bool usedFallback, bool low) = ZimBotReplyComposer.Compose(ZimBotMessages.Unavailable);
        Assert.Equal(ZimBotMessages.Unavailable, reply);
        Assert.True(usedFallback);
        Assert.False(low);
    }
}
