import { apiFetch } from "@/lib/api/client";
import { assembleDashboard, mapLeaderboard, mapPoints, mapStudentBadges, mapStudentProgress } from "@/lib/dashboard/mapDashboard";
import type { StudentDashboardData } from "@/lib/dashboard/types";

export async function fetchStudentDashboard(
  studentId: string,
  tenantId: string | undefined,
): Promise<StudentDashboardData> {
  return loadStudentDashboard(studentId, tenantId, (path) => apiFetch<Record<string, unknown>>(path));
}

export async function loadStudentDashboard(
  studentId: string,
  tenantId: string | undefined,
  getJson: (path: string) => Promise<Record<string, unknown>>,
): Promise<StudentDashboardData> {
  const progressPath = `/api/v1/students/${studentId}/progress`;
  const pointsPath = `/api/v1/gamification/${studentId}/points`;
  const badgesPath = `/api/v1/gamification/${studentId}/badges`;
  const leaderboardPath = tenantId
    ? `/api/v1/gamification/leaderboard/${tenantId}?limit=100`
    : null;

  const [progressRaw, pointsRaw, badgesRaw, leaderboardRaw] = await Promise.all([
    getJson(progressPath),
    getJson(pointsPath),
    getJson(badgesPath),
    leaderboardPath ? getJson(leaderboardPath) : Promise.resolve({ entries: [] }),
  ]);

  return assembleDashboard(
    studentId,
    mapStudentProgress(progressRaw),
    mapPoints(pointsRaw),
    mapStudentBadges(badgesRaw),
    mapLeaderboard(leaderboardRaw),
  );
}
