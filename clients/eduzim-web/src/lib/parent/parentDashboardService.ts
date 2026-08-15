import { apiFetch } from "@/lib/api/client";
import {
  mapBadgesPayload,
  mapParentDashboard,
  mapWeeklySummary,
  withStudentExtras,
} from "@/lib/parent/mapParentDashboard";
import type { ParentDashboardData } from "@/lib/parent/types";

export async function fetchParentDashboard(
  parentId: string,
  tenantId: string | undefined,
): Promise<ParentDashboardData> {
  return loadParentDashboard(parentId, tenantId, (path) => apiFetch<Record<string, unknown>>(path));
}

export async function loadParentDashboard(
  parentId: string,
  tenantId: string | undefined,
  getJson: (path: string) => Promise<Record<string, unknown>>,
): Promise<ParentDashboardData> {
  const dashboardPath = tenantId
    ? `/api/v1/parents/${parentId}/dashboard?tenantId=${encodeURIComponent(tenantId)}`
    : `/api/v1/parents/${parentId}/dashboard`;
  const dashboard = mapParentDashboard(await getJson(dashboardPath));
  const students = await Promise.all(
    dashboard.students.map(async (student) => {
      const badgesPath = `/api/v1/gamification/${student.studentId}/badges`;
      const summaryPath = `/api/v1/adaptive/${student.studentId}/summary`;
      const [badgesRaw, summaryRaw] = await Promise.all([getJson(badgesPath), getJson(summaryPath)]);
      return withStudentExtras(student, mapBadgesPayload(badgesRaw), mapWeeklySummary(summaryRaw));
    }),
  );

  return { parentId: dashboard.parentId, students };
}

export async function setDailyScreenTimeLimit(
  studentId: string,
  dailyScreenTimeLimitSeconds: number | null,
  tenantId: string | undefined,
): Promise<void> {
  const path = tenantId
    ? `/api/v1/students/${studentId}/screen-time?tenantId=${encodeURIComponent(tenantId)}`
    : `/api/v1/students/${studentId}/screen-time`;
  await apiFetch(path, {
    method: "PUT",
    body: JSON.stringify({ dailyScreenTimeLimitSeconds }),
  });
}
