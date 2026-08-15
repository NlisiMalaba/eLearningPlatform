import type { QuizQuestion } from "@/lib/content/types";

export function hasAnswerKey(question: QuizQuestion): boolean {
  return question.correctOptionIndex !== undefined || Boolean(question.correctShortAnswer);
}

export function isQuizAnswerCorrect(question: QuizQuestion, answer: string): boolean {
  if (question.type === "ShortAnswer") {
    return answer.trim().toLowerCase() === (question.correctShortAnswer ?? "").trim().toLowerCase();
  }

  return answer === String(question.correctOptionIndex);
}
