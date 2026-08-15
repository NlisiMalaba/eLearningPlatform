import { describe, expect, it } from "vitest";
import { parseQuizPayload } from "@/lib/content/parseQuizPayload";
import { hasAnswerKey, isQuizAnswerCorrect } from "@/lib/content/quizScoring";

describe("parseQuizPayload", () => {
  it("reads multiple-choice, true/false, and short-answer questions", () => {
    const quiz = parseQuizPayload({
      questions: [
        {
          id: "q1",
          type: "MultipleChoice",
          prompt: "What is 2 + 2?",
          options: ["3", "4"],
          correctOptionIndex: 1,
        },
        { type: 1, text: "Harare is the capital.", correctOptionIndex: 0 },
        { id: "q3", type: "ShortAnswer", prompt: "River", correctShortAnswer: "Zambezi" },
      ],
    });

    expect(quiz.questions).toHaveLength(3);
    expect(quiz.questions[0]?.options).toEqual(["3", "4"]);
    expect(quiz.questions[1]?.type).toBe("TrueFalse");
    expect(quiz.questions[1]?.options).toEqual(["True", "False"]);
    expect(quiz.questions[2]?.correctShortAnswer).toBe("Zambezi");
  });

  it("returns an empty quiz for invalid payloads", () => {
    expect(parseQuizPayload(null).questions).toEqual([]);
    expect(parseQuizPayload({ questions: ["nope"] }).questions).toEqual([]);
  });
});

describe("quiz scoring", () => {
  it("checks option index and short-answer keys", () => {
    const multipleChoice = {
      id: "q1",
      type: "MultipleChoice" as const,
      prompt: "Capital",
      options: ["Harare", "Bulawayo"],
      correctOptionIndex: 0,
    };
    const shortAnswer = {
      id: "q2",
      type: "ShortAnswer" as const,
      prompt: "River",
      options: [],
      correctShortAnswer: "Zambezi",
    };

    expect(hasAnswerKey(multipleChoice)).toBe(true);
    expect(isQuizAnswerCorrect(multipleChoice, "0")).toBe(true);
    expect(isQuizAnswerCorrect(multipleChoice, "1")).toBe(false);
    expect(isQuizAnswerCorrect(shortAnswer, " zambezi ")).toBe(true);
  });
});
