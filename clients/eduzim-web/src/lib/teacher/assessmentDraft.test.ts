import { describe, expect, it } from "vitest";
import {
  createQuestionDraft,
  minutesToSeconds,
  questionTypeIndex,
  validateAssessmentDraft,
} from "@/lib/teacher/assessmentDraft";

describe("assessment draft validation", () => {
  it("accepts multiple-choice, true/false, and short-answer questions with an optional time limit", () => {
    const draft = {
      title: "Fractions",
      moduleId: "mod-1",
      passingScorePercent: 60,
      timed: true,
      timeLimitMinutes: 10,
      questions: [
        {
          ...createQuestionDraft("MultipleChoice"),
          text: "1/2 + 1/2",
          optionTexts: ["1", "2"],
          correctOptionIndex: 0,
        },
        {
          ...createQuestionDraft("TrueFalse"),
          text: "1/2 is less than 1",
          correctOptionIndex: 0,
        },
        {
          ...createQuestionDraft("ShortAnswer"),
          text: "Write one half as a fraction",
          correctShortAnswer: "1/2",
        },
      ],
    };

    expect(validateAssessmentDraft(draft)).toBeNull();
    expect(minutesToSeconds(draft.timeLimitMinutes)).toBe(600);
    expect(questionTypeIndex("TrueFalse")).toBe(1);
  });

  it("rejects missing title, empty questions, and invalid multiple-choice options", () => {
    expect(
      validateAssessmentDraft({
        title: "",
        moduleId: "mod-1",
        passingScorePercent: 60,
        timed: false,
        timeLimitMinutes: 10,
        questions: [createQuestionDraft("ShortAnswer")],
      }),
    ).toBe("title");

    expect(
      validateAssessmentDraft({
        title: "Quiz",
        moduleId: "mod-1",
        passingScorePercent: 60,
        timed: false,
        timeLimitMinutes: 10,
        questions: [],
      }),
    ).toBe("questions");

    const mc = createQuestionDraft("MultipleChoice");
    mc.text = "Pick one";
    mc.optionTexts = ["only-one"];
    expect(
      validateAssessmentDraft({
        title: "Quiz",
        moduleId: "mod-1",
        passingScorePercent: 60,
        timed: false,
        timeLimitMinutes: 10,
        questions: [mc],
      }),
    ).toBe("mc-options");
  });
});
