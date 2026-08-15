import { describe, expect, it } from "vitest";
import { parseQuizPayload } from "@/lib/content/parseQuizPayload";

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
