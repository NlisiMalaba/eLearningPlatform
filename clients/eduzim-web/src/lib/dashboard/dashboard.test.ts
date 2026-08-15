import { describe, expect, it } from "vitest";
import { buildRecentActivity, findContinueModule, findLeaderboardRank } from "@/lib/dashboard/activity";
import { mapStudentProgress } from "@/lib/dashboard/mapDashboard";
import { clampPercent, parseBadgeType } from "@/lib/dashboard/parse";
import { loadStudentDashboard } from "@/lib/dashboard/dashboardService";
import type { ModuleProgress, SubjectProgress } from "@/lib/dashboard/types";

const mathModule: ModuleProgress = {
  moduleId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  title: "Fractions",
  sequenceOrder: 1,
  isCompleted: true,
  isAccessible: true,
  completedAt: "2026-08-14T10:00:00.000Z",
};

const nextModule: ModuleProgress = {
  moduleId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  title: "Decimals",
  sequenceOrder: 2,
  isCompleted: false,
  isAccessible: true,
  completedAt: null,
};

const math: SubjectProgress = {
  subject: "Mathematics",
  progressPercent: 50,
  modules: [mathModule, nextModule],
};

describe("student dashboard mapping", () => {
  it("clamps subject percents and parses badge types", () => {
    expect(clampPercent(149.6)).toBe(100);
    expect(clampPercent(-4)).toBe(0);
    expect(parseBadgeType(0)).toBe("FirstModule");
    expect(parseBadgeType("SubjectMastery")).toBe("SubjectMastery");
  });

  it("builds recent activity from completed modules and badges", () => {
    const activity = buildRecentActivity(math.modules ? [math] : [], [
      {
        badgeId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
        type: "FirstModule",
        earnedAt: "2026-08-14T11:00:00.000Z",
      },
    ]);

    expect(activity[0]?.kind).toBe("BadgeEarned");
    expect(activity[1]?.kind).toBe("ModuleCompleted");
    expect(activity[1]?.title).toBe("Fractions");
  });

  it("finds the next accessible module and leaderboard rank", () => {
    expect(findContinueModule([math])?.title).toBe("Decimals");
    expect(
      findLeaderboardRank("student-1", [
        { studentId: "other", rank: 1 },
        { studentId: "student-1", rank: 4 },
      ]),
    ).toBe(4);
    expect(findLeaderboardRank("missing", [{ studentId: "other", rank: 1 }])).toBeNull();
  });

  it("maps progress JSON from the API", () => {
    const progress = mapStudentProgress({
      studentId: "s1",
      subjects: [
        {
          subject: "Mathematics",
          progressPercent: 50,
          modules: [
            {
              moduleId: mathModule.moduleId,
              title: "Fractions",
              sequenceOrder: 1,
              isCompleted: true,
              isAccessible: true,
              completedAt: mathModule.completedAt,
            },
          ],
        },
      ],
    });

    expect(progress.subjects[0]?.progressPercent).toBe(50);
    expect(progress.subjects[0]?.modules[0]?.title).toBe("Fractions");
  });

  it("assembles dashboard data from parallel API payloads", async () => {
    const studentId = "11111111-1111-1111-1111-111111111111";
    const tenantId = "22222222-2222-2222-2222-222222222222";
    const data = await loadStudentDashboard(studentId, tenantId, async (path) => {
      if (path.includes("/progress")) {
        return {
          studentId,
          subjects: [{ subject: "Mathematics", progressPercent: 50, modules: [mathModule] }],
        };
      }

      if (path.includes("/points")) {
        return { studentId, totalPoints: 120 };
      }

      if (path.includes("/badges")) {
        return {
          studentId,
          badges: [
            {
              badgeId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
              type: "FirstModule",
              earnedAt: "2026-08-14T11:00:00.000Z",
            },
          ],
        };
      }

      return {
        tenantId,
        entries: [{ studentId, totalPoints: 120, rank: 2, displayName: "You" }],
      };
    });

    expect(data.totalPoints).toBe(120);
    expect(data.leaderboardRank).toBe(2);
    expect(data.badges).toHaveLength(1);
    expect(data.recentActivity.some((item) => item.kind === "ModuleCompleted")).toBe(true);
    expect(data.recentActivity.some((item) => item.kind === "BadgeEarned")).toBe(true);
  });
});
