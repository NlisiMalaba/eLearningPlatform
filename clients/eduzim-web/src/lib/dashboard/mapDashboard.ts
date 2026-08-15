import { clampPercent, parseBadgeType } from "@/lib/dashboard/parse";
import { buildRecentActivity, findContinueModule, findLeaderboardRank } from "@/lib/dashboard/activity";
import type {
  LeaderboardEntry,
  ModuleProgress,
  StudentBadge,
  StudentDashboardData,
  StudentProgress,
  SubjectProgress,
} from "@/lib/dashboard/types";

export function mapStudentProgress(raw: Record<string, unknown>): StudentProgress {
  const subjects = Array.isArray(raw.subjects) ? raw.subjects.flatMap(mapSubject) : [];
  return {
    studentId: readString(raw.studentId) ?? "",
    subjects,
  };
}

export function mapStudentBadges(raw: Record<string, unknown>): StudentBadge[] {
  const items = Array.isArray(raw.badges) ? raw.badges : [];
  return items.flatMap(mapBadge);
}

export function mapLeaderboard(raw: Record<string, unknown>): LeaderboardEntry[] {
  const items = Array.isArray(raw.entries) ? raw.entries : [];
  return items.flatMap(mapLeaderboardEntry);
}

export function mapPoints(raw: Record<string, unknown>): number {
  return typeof raw.totalPoints === "number" ? Math.max(0, raw.totalPoints) : 0;
}

export function assembleDashboard(
  studentId: string,
  progress: StudentProgress,
  totalPoints: number,
  badges: StudentBadge[],
  leaderboard: LeaderboardEntry[],
): StudentDashboardData {
  return {
    studentId,
    subjects: progress.subjects,
    recentActivity: buildRecentActivity(progress.subjects, badges),
    totalPoints,
    badges,
    leaderboardRank: findLeaderboardRank(studentId, leaderboard),
    continueModule: findContinueModule(progress.subjects),
  };
}

function mapSubject(value: unknown): SubjectProgress[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const subject = readString(raw.subject);
  if (!subject) {
    return [];
  }

  const modules = Array.isArray(raw.modules) ? raw.modules.flatMap(mapModule) : [];
  return [
    {
      subject,
      progressPercent: clampPercent(typeof raw.progressPercent === "number" ? raw.progressPercent : 0),
      modules,
    },
  ];
}

function mapModule(value: unknown): ModuleProgress[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const moduleId = readString(raw.moduleId);
  if (!moduleId) {
    return [];
  }

  return [
    {
      moduleId,
      title: readString(raw.title) ?? "",
      sequenceOrder: typeof raw.sequenceOrder === "number" ? raw.sequenceOrder : 0,
      isCompleted: raw.isCompleted === true,
      isAccessible: raw.isAccessible === true,
      completedAt: readString(raw.completedAt) ?? null,
    },
  ];
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

function mapLeaderboardEntry(value: unknown): LeaderboardEntry[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const studentId = readString(raw.studentId);
  if (!studentId || typeof raw.rank !== "number") {
    return [];
  }

  return [
    {
      studentId,
      totalPoints: typeof raw.totalPoints === "number" ? raw.totalPoints : 0,
      rank: raw.rank,
      displayName: readString(raw.displayName) ?? "",
    },
  ];
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}
