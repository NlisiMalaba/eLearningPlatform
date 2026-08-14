import { BADGE_TYPES, type BadgeType } from "@/lib/dashboard/types";

const BADGE_BY_INDEX: Record<number, BadgeType> = {
  0: "FirstModule",
  1: "FiveConsecutiveDays",
  2: "SubjectMastery",
  3: "GradeCompletion",
};

export function parseBadgeType(value: unknown): BadgeType | null {
  if (typeof value === "number" && Number.isInteger(value)) {
    return BADGE_BY_INDEX[value] ?? null;
  }

  if (typeof value === "string" && (BADGE_TYPES as readonly string[]).includes(value)) {
    return value as BadgeType;
  }

  return null;
}

export function clampPercent(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }

  return Math.min(100, Math.max(0, Math.round(value)));
}
