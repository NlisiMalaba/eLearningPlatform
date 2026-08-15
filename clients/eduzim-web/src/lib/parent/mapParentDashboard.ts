import { clampPercent, parseBadgeType } from "@/lib/dashboard/parse";
import type { RecentActivity, StudentBadge } from "@/lib/dashboard/types";
import { parseGradeLevel } from "@/lib/teacher/grades";
import type { LinkedStudentDashboard, ParentDashboardData, WeeklySummary } from "@/lib/parent/types";

export function mapParentDashboard(raw: Record<string, unknown>): ParentDashboardData {
  const students = Array.isArray(raw.students) ? raw.students.flatMap(mapLinkedStudent) : [];
  return {
    parentId: readString(raw.parentId) ?? "",
    students,
  };
}

export function mapWeeklySummary(raw: Record<string, unknown>): WeeklySummary {
  return {
    isoYear: readNumber(raw.isoYear),
    isoWeek: readNumber(raw.isoWeek),
    weekStartUtc: readString(raw.weekStartUtc) ?? "",
    weekEndUtc: readString(raw.weekEndUtc) ?? "",
    assessmentsSubmittedCount: readNumber(raw.assessmentsSubmittedCount),
    averageScorePercent:
      typeof raw.averageScorePercent === "number" && Number.isFinite(raw.averageScorePercent)
        ? clampPercent(raw.averageScorePercent)
        : null,
    totalAssessmentTimeSeconds: readNumber(raw.totalAssessmentTimeSeconds),
    modulesMarkedCompleteInWeek: readNumber(raw.modulesMarkedCompleteInWeek),
    badgesEarnedInWeek: 0,
  };
}

export function withStudentExtras(
  student: LinkedStudentDashboard,
  badges: StudentBadge[],
  weeklySummary: WeeklySummary | null,
): LinkedStudentDashboard {
  const badgesEarnedInWeek = countBadgesInWeek(badges, weeklySummary?.weekStartUtc);
  return {
    ...student,
    badges,
    weeklySummary: weeklySummary
      ? { ...weeklySummary, badgesEarnedInWeek }
      : null,
  };
}

function mapLinkedStudent(value: unknown): LinkedStudentDashboard[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const studentId = readString(raw.studentId);
  if (!studentId) {
    return [];
  }

  const activity = Array.isArray(raw.recentActivity) ? raw.recentActivity.flatMap(mapActivity) : [];
  const subjects = Array.isArray(raw.subjects)
    ? raw.subjects.filter((item): item is string => typeof item === "string" && item.length > 0)
    : [];

  return [
    {
      studentId,
      displayName: readString(raw.displayName) ?? "",
      currentGrade: parseGradeLevel(raw.currentGrade),
      subjects,
      recentActivity: activity,
      overallProgressPercent: clampPercent(
        typeof raw.overallProgressPercent === "number" ? raw.overallProgressPercent : 0,
      ),
      dailyScreenTimeLimitSeconds:
        typeof raw.dailyScreenTimeLimitSeconds === "number" && raw.dailyScreenTimeLimitSeconds > 0
          ? raw.dailyScreenTimeLimitSeconds
          : null,
      badges: [],
      weeklySummary: null,
    },
  ];
}

function mapActivity(value: unknown): RecentActivity[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const kind = raw.kind === "BadgeEarned" ? "BadgeEarned" : raw.kind === "ModuleCompleted" ? "ModuleCompleted" : null;
  const occurredAt = readString(raw.occurredAt);
  if (!kind || !occurredAt) {
    return [];
  }

  const title = readString(raw.title) ?? "";
  return [
    {
      id: `${kind}:${title}:${occurredAt}`,
      kind,
      title,
      occurredAt,
    },
  ];
}

export function mapBadgesPayload(raw: Record<string, unknown>): StudentBadge[] {
  const items = Array.isArray(raw.badges) ? raw.badges : [];
  return items.flatMap(mapBadge);
}

function mapBadge(value: unknown): StudentBadge[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const badgeId = readString(raw.badgeId);
  const type = parseBadgeType(raw.type);
  const earnedAt = readString(raw.earnedAt);
  if (!badgeId || !type || !earnedAt) {
    return [];
  }

  return [{ badgeId, type, earnedAt }];
}

function countBadgesInWeek(badges: readonly StudentBadge[], weekStartUtc: string | undefined): number {
  if (!weekStartUtc) {
    return 0;
  }

  const start = Date.parse(weekStartUtc);
  if (Number.isNaN(start)) {
    return 0;
  }

  return badges.filter((badge) => Date.parse(badge.earnedAt) >= start).length;
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

function readNumber(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}
