import { apiFetch } from "@/lib/api/client";
import {
  minutesToSeconds,
  questionTypeIndex,
  type AssessmentDraft,
} from "@/lib/teacher/assessmentDraft";

export type AssessmentListItem = {
  id: string;
  title: string;
  moduleId: string;
  timeLimitSeconds: number | null;
  passingScorePercent: number;
  questionCount: number;
};

export type SchoolClassItem = {
  id: string;
  name: string;
};

export type StudentResultRow = {
  studentId: string;
  scorePercent: number | null;
  hasCompleted: boolean;
  timeTakenSeconds: number | null;
};

export type ClassResults = {
  schoolClassId: string;
  schoolClassName: string;
  enrolledStudentCount: number;
  completedCount: number;
  completionRatePercent: number;
  averageTimeTakenSecondsAmongCompleters: number | null;
  students: StudentResultRow[];
};

export async function listAssessments(): Promise<AssessmentListItem[]> {
  const raw = await apiFetch<unknown[]>("/api/v1/assessments");
  return Array.isArray(raw) ? raw.flatMap(mapAssessment) : [];
}

export async function listSchoolClasses(): Promise<SchoolClassItem[]> {
  const raw = await apiFetch<unknown[]>("/api/v1/classes");
  return Array.isArray(raw) ? raw.flatMap(mapClass) : [];
}

export async function createAssessment(draft: AssessmentDraft): Promise<string> {
  const raw = await apiFetch<Record<string, unknown>>("/api/v1/assessments", {
    method: "POST",
    body: JSON.stringify(toCreatePayload(draft)),
  });
  return readString(raw.assessmentId) ?? "";
}

export async function assignAssessment(
  assessmentId: string,
  schoolClassId: string,
  dueAtUtc: string,
): Promise<void> {
  await apiFetch(`/api/v1/assessments/${assessmentId}/assign`, {
    method: "POST",
    body: JSON.stringify({ schoolClassId, dueAtUtc }),
  });
}

export async function getClassResults(
  assessmentId: string,
  schoolClassId: string,
): Promise<ClassResults> {
  const raw = await apiFetch<Record<string, unknown>>(
    `/api/v1/assessments/${assessmentId}/class-results?schoolClassId=${schoolClassId}`,
  );
  return mapClassResults(raw);
}

export function toCreatePayload(draft: AssessmentDraft): Record<string, unknown> {
  return {
    moduleId: draft.moduleId,
    title: draft.title.trim(),
    timeLimitSeconds: draft.timed ? minutesToSeconds(draft.timeLimitMinutes) : null,
    passingScorePercent: draft.passingScorePercent,
    questions: draft.questions.map((question) => ({
      type: questionTypeIndex(question.type),
      text: question.text.trim(),
      points: question.points,
      optionTexts: question.type === "ShortAnswer" ? null : question.optionTexts,
      correctOptionIndex: question.type === "ShortAnswer" ? null : question.correctOptionIndex,
      correctShortAnswer: question.type === "ShortAnswer" ? question.correctShortAnswer.trim() : null,
    })),
  };
}

export function mapClassResults(raw: Record<string, unknown>): ClassResults {
  const students = Array.isArray(raw.students) ? raw.students : [];
  return {
    schoolClassId: readString(raw.schoolClassId) ?? "",
    schoolClassName: readString(raw.schoolClassName) ?? "",
    enrolledStudentCount: readNumber(raw.enrolledStudentCount),
    completedCount: readNumber(raw.completedCount),
    completionRatePercent: readNumber(raw.completionRatePercent),
    averageTimeTakenSecondsAmongCompleters: readNullableNumber(raw.averageTimeTakenSecondsAmongCompleters),
    students: students.flatMap(mapStudentRow),
  };
}

function mapAssessment(value: unknown): AssessmentListItem[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const id = readString(raw.id);
  const title = readString(raw.title);
  const moduleId = readString(raw.moduleId);
  if (!id || !title || !moduleId) {
    return [];
  }

  return [
    {
      id,
      title,
      moduleId,
      timeLimitSeconds: readNullableNumber(raw.timeLimitSeconds),
      passingScorePercent: readNumber(raw.passingScorePercent),
      questionCount: readNumber(raw.questionCount),
    },
  ];
}

function mapClass(value: unknown): SchoolClassItem[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const id = readString(raw.id);
  const name = readString(raw.name);
  return id && name ? [{ id, name }] : [];
}

function mapStudentRow(value: unknown): StudentResultRow[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const studentId = readString(raw.studentId);
  if (!studentId) {
    return [];
  }

  return [
    {
      studentId,
      scorePercent: readNullableNumber(raw.scorePercent),
      hasCompleted: raw.hasCompleted === true,
      timeTakenSeconds: readNullableNumber(raw.timeTakenSeconds),
    },
  ];
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

function readNumber(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function readNullableNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}
