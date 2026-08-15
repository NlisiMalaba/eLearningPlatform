import type { QuizQuestionType } from "@/lib/content/types";

export const ASSESSMENT_QUESTION_TYPES = [
  "MultipleChoice",
  "TrueFalse",
  "ShortAnswer",
] as const satisfies readonly QuizQuestionType[];

export type AssessmentQuestionType = (typeof ASSESSMENT_QUESTION_TYPES)[number];

export type AssessmentQuestionDraft = {
  id: string;
  type: AssessmentQuestionType;
  text: string;
  points: number;
  optionTexts: string[];
  correctOptionIndex: number | null;
  correctShortAnswer: string;
};

export type AssessmentDraft = {
  title: string;
  moduleId: string;
  passingScorePercent: number;
  timed: boolean;
  timeLimitMinutes: number;
  questions: AssessmentQuestionDraft[];
};

export type AssessmentDraftIssue =
  | "title"
  | "module"
  | "questions"
  | "points"
  | "time"
  | "mc-options"
  | "mc-correct"
  | "tf-correct"
  | "short";

export function createQuestionDraft(type: AssessmentQuestionType): AssessmentQuestionDraft {
  const id =
    typeof globalThis.crypto?.randomUUID === "function"
      ? globalThis.crypto.randomUUID()
      : `q-${Date.now()}-${Math.random().toString(16).slice(2)}`;
  return {
    id,
    type,
    text: "",
    points: 1,
    optionTexts: type === "MultipleChoice" ? ["", ""] : type === "TrueFalse" ? ["True", "False"] : [],
    correctOptionIndex: type === "ShortAnswer" ? null : 0,
    correctShortAnswer: "",
  };
}

export function questionTypeIndex(type: AssessmentQuestionType): number {
  return ASSESSMENT_QUESTION_TYPES.indexOf(type);
}

export function minutesToSeconds(minutes: number): number {
  return Math.round(minutes * 60);
}

export function validateAssessmentDraft(draft: AssessmentDraft): AssessmentDraftIssue | null {
  if (!draft.title.trim()) {
    return "title";
  }

  if (!draft.moduleId) {
    return "module";
  }

  if (draft.questions.length === 0) {
    return "questions";
  }

  if (draft.timed && (draft.timeLimitMinutes < 1 || draft.timeLimitMinutes > 1440)) {
    return "time";
  }

  for (const question of draft.questions) {
    const issue = validateQuestion(question);
    if (issue) {
      return issue;
    }
  }

  return null;
}

export function validateQuestion(question: AssessmentQuestionDraft): AssessmentDraftIssue | null {
  if (!question.text.trim() || question.points < 1) {
    return "points";
  }

  if (question.type === "MultipleChoice") {
    const options = question.optionTexts.map((item) => item.trim()).filter((item) => item.length > 0);
    if (options.length < 2) {
      return "mc-options";
    }

    if (
      question.correctOptionIndex === null ||
      question.correctOptionIndex < 0 ||
      question.correctOptionIndex >= question.optionTexts.length ||
      !question.optionTexts[question.correctOptionIndex]?.trim()
    ) {
      return "mc-correct";
    }
  }

  if (question.type === "TrueFalse" && question.correctOptionIndex !== 0 && question.correctOptionIndex !== 1) {
    return "tf-correct";
  }

  if (question.type === "ShortAnswer" && !question.correctShortAnswer.trim()) {
    return "short";
  }

  return null;
}
