import {
  QUIZ_QUESTION_TYPES,
  type QuizPayload,
  type QuizQuestion,
  type QuizQuestionType,
} from "@/lib/content/types";

export function parseQuizPayload(value: unknown): QuizPayload {
  if (!value || typeof value !== "object") {
    return { questions: [] };
  }

  const record = value as Record<string, unknown>;
  const rawQuestions = Array.isArray(record.questions) ? record.questions : [];
  return { questions: rawQuestions.flatMap((item, index) => parseQuestion(item, index)) };
}

function parseQuestion(value: unknown, index: number): QuizQuestion[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const record = value as Record<string, unknown>;
  const prompt = readString(record.prompt) ?? readString(record.text);
  if (!prompt) {
    return [];
  }

  const type = parseQuestionType(record.type);
  const options = parseOptions(record.options ?? record.optionTexts, type);
  const question: QuizQuestion = {
    id: readString(record.id) ?? `question-${index + 1}`,
    type,
    prompt,
    options,
  };

  if (typeof record.correctOptionIndex === "number") {
    question.correctOptionIndex = record.correctOptionIndex;
  }

  const shortAnswer = readString(record.correctShortAnswer);
  if (shortAnswer) {
    question.correctShortAnswer = shortAnswer;
  }

  return [question];
}

function parseQuestionType(value: unknown): QuizQuestionType {
  if (typeof value === "number") {
    return QUIZ_QUESTION_TYPES[value] ?? "MultipleChoice";
  }

  if (typeof value === "string" && (QUIZ_QUESTION_TYPES as readonly string[]).includes(value)) {
    return value as QuizQuestionType;
  }

  return "MultipleChoice";
}

function parseOptions(value: unknown, type: QuizQuestionType): string[] {
  if (Array.isArray(value)) {
    return value.filter((item): item is string => typeof item === "string" && item.length > 0);
  }

  if (type === "TrueFalse") {
    return ["True", "False"];
  }

  return [];
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.trim().length > 0 ? value : undefined;
}
