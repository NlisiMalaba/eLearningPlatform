import type { GradeLevel } from "@/lib/teacher/grades";
import type { RecentActivity, StudentBadge } from "@/lib/dashboard/types";

export type WeeklySummary = {
  isoYear: number;
  isoWeek: number;
  weekStartUtc: string;
  weekEndUtc: string;
  assessmentsSubmittedCount: number;
  averageScorePercent: number | null;
  totalAssessmentTimeSeconds: number;
  modulesMarkedCompleteInWeek: number;
  badgesEarnedInWeek: number;
};

export type LinkedStudentDashboard = {
  studentId: string;
  displayName: string;
  currentGrade: GradeLevel;
  subjects: string[];
  recentActivity: RecentActivity[];
  overallProgressPercent: number;
  dailyScreenTimeLimitSeconds: number | null;
  badges: StudentBadge[];
  weeklySummary: WeeklySummary | null;
};

export type ParentDashboardData = {
  parentId: string;
  students: LinkedStudentDashboard[];
};
