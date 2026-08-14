export const GRADE_LEVELS = [
  "EcdGrade0",
  "EcdGrade1",
  "Grade1",
  "Grade2",
  "Grade3",
  "Grade4",
  "Grade5",
  "Grade6",
  "Grade7",
] as const;

export type GradeLevel = (typeof GRADE_LEVELS)[number];

const GRADE_BY_INDEX: Record<number, GradeLevel> = {
  0: "EcdGrade0",
  1: "EcdGrade1",
  2: "Grade1",
  3: "Grade2",
  4: "Grade3",
  5: "Grade4",
  6: "Grade5",
  7: "Grade6",
  8: "Grade7",
};

export function parseGradeLevel(value: unknown): GradeLevel {
  if (typeof value === "number" && GRADE_BY_INDEX[value]) {
    return GRADE_BY_INDEX[value];
  }

  if (typeof value === "string" && (GRADE_LEVELS as readonly string[]).includes(value)) {
    return value as GradeLevel;
  }

  return "Grade1";
}

export function gradeLevelIndex(grade: GradeLevel): number {
  return GRADE_LEVELS.indexOf(grade);
}
