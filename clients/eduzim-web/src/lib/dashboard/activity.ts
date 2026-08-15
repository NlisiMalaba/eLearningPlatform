import type {
  ModuleProgress,
  RecentActivity,
  StudentBadge,
  SubjectProgress,
} from "@/lib/dashboard/types";

export const RECENT_ACTIVITY_LIMIT = 10;

export function buildRecentActivity(
  subjects: readonly SubjectProgress[],
  badges: readonly StudentBadge[],
): RecentActivity[] {
  const fromModules: RecentActivity[] = subjects.flatMap((subject) =>
    subject.modules.flatMap((module) => {
      if (!module.isCompleted || !module.completedAt) {
        return [];
      }

      return [
        {
          id: `module:${module.moduleId}`,
          kind: "ModuleCompleted" as const,
          title: module.title,
          occurredAt: module.completedAt,
        },
      ];
    }),
  );

  const fromBadges: RecentActivity[] = badges.map((badge) => ({
    id: `badge:${badge.badgeId}`,
    kind: "BadgeEarned" as const,
    title: badge.type,
    occurredAt: badge.earnedAt,
  }));

  return [...fromModules, ...fromBadges]
    .sort((a, b) => Date.parse(b.occurredAt) - Date.parse(a.occurredAt))
    .slice(0, RECENT_ACTIVITY_LIMIT);
}

export function findContinueModule(subjects: readonly SubjectProgress[]): ModuleProgress | null {
  for (const subject of subjects) {
    const next = subject.modules.find((module) => module.isAccessible && !module.isCompleted);
    if (next) {
      return next;
    }
  }

  return null;
}

export function findLeaderboardRank(
  studentId: string,
  entries: readonly { studentId: string; rank: number }[],
): number | null {
  const match = entries.find((entry) => entry.studentId === studentId);
  return match ? match.rank : null;
}
