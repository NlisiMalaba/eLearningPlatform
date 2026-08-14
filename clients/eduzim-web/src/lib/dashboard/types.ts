export const BADGE_TYPES = [
  "FirstModule",
  "FiveConsecutiveDays",
  "SubjectMastery",
  "GradeCompletion",
] as const;

export type BadgeType = (typeof BADGE_TYPES)[number];

export type SubjectProgress = {
  subject: string;
  progressPercent: number;
  modules: ModuleProgress[];
};

export type ModuleProgress = {
  moduleId: string;
  title: string;
  sequenceOrder: number;
  isCompleted: boolean;
  isAccessible: boolean;
  completedAt: string | null;
};

export type StudentProgress = {
  studentId: string;
  subjects: SubjectProgress[];
};

export type StudentBadge = {
  badgeId: string;
  type: BadgeType;
  earnedAt: string;
};

export type ActivityKind = "ModuleCompleted" | "BadgeEarned";

export type RecentActivity = {
  id: string;
  kind: ActivityKind;
  title: string;
  occurredAt: string;
};

export type LeaderboardEntry = {
  studentId: string;
  totalPoints: number;
  rank: number;
  displayName: string;
};

export type StudentDashboardData = {
  studentId: string;
  subjects: SubjectProgress[];
  recentActivity: RecentActivity[];
  totalPoints: number;
  badges: StudentBadge[];
  leaderboardRank: number | null;
  continueModule: ModuleProgress | null;
};
