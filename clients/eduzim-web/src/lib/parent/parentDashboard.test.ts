import { describe, expect, it } from "vitest";
import { loadParentDashboard } from "@/lib/parent/parentDashboardService";
import { mapParentDashboard, mapWeeklySummary, withStudentExtras } from "@/lib/parent/mapParentDashboard";

describe("parent dashboard mapping", () => {
  it("maps grade, subjects, activity, progress, and screen time", () => {
    const studentId = "11111111-1111-1111-1111-111111111111";
    const data = mapParentDashboard({
      parentId: "parent-1",
      students: [
        {
          studentId,
          displayName: "Tariro Moyo",
          currentGrade: 3,
          subjects: ["Mathematics", "English"],
          recentActivity: [
            {
              kind: "ModuleCompleted",
              title: "Fractions",
              occurredAt: "2026-08-14T10:00:00.000Z",
            },
          ],
          overallProgressPercent: 42,
          dailyScreenTimeLimitSeconds: 3600,
        },
      ],
    });

    expect(data.students).toHaveLength(1);
    expect(data.students[0]?.currentGrade).toBe("Grade2");
    expect(data.students[0]?.subjects).toEqual(["Mathematics", "English"]);
    expect(data.students[0]?.overallProgressPercent).toBe(42);
    expect(data.students[0]?.dailyScreenTimeLimitSeconds).toBe(3600);
    expect(data.students[0]?.recentActivity[0]?.kind).toBe("ModuleCompleted");
  });

  it("counts badges earned in the current week on the weekly summary", () => {
    const summary = mapWeeklySummary({
      isoYear: 2026,
      isoWeek: 33,
      weekStartUtc: "2026-08-10T00:00:00.000Z",
      weekEndUtc: "2026-08-16T23:59:59.000Z",
      assessmentsSubmittedCount: 2,
      averageScorePercent: 88.4,
      totalAssessmentTimeSeconds: 1200,
      modulesMarkedCompleteInWeek: 1,
    });
    const student = mapParentDashboard({
      parentId: "p",
      students: [{ studentId: "s1", currentGrade: "Grade1", subjects: [], recentActivity: [], overallProgressPercent: 10 }],
    }).students[0];
    if (!student) {
      throw new Error("expected mapped student");
    }

    const merged = withStudentExtras(
      student,
      [
        {
          badgeId: "b1",
          type: "FirstModule",
          earnedAt: "2026-08-12T09:00:00.000Z",
        },
        {
          badgeId: "b2",
          type: "SubjectMastery",
          earnedAt: "2026-08-01T09:00:00.000Z",
        },
      ],
      summary,
    );

    expect(merged.weeklySummary?.averageScorePercent).toBe(88);
    expect(merged.weeklySummary?.badgesEarnedInWeek).toBe(1);
    expect(merged.badges).toHaveLength(2);
  });

  it("loads dashboard, badges, and weekly summary together", async () => {
    const parentId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const studentId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    const data = await loadParentDashboard(parentId, undefined, async (path) => {
      if (path.includes("/dashboard")) {
        return {
          parentId,
          students: [
            {
              studentId,
              displayName: "Chipo",
              currentGrade: "Grade3",
              subjects: ["Science"],
              recentActivity: [],
              overallProgressPercent: 20,
              dailyScreenTimeLimitSeconds: null,
            },
          ],
        };
      }

      if (path.includes("/badges")) {
        return {
          studentId,
          badges: [
            {
              badgeId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
              type: "FirstModule",
              earnedAt: "2026-08-12T09:00:00.000Z",
            },
          ],
        };
      }

      return {
        isoYear: 2026,
        isoWeek: 33,
        weekStartUtc: "2026-08-10T00:00:00.000Z",
        weekEndUtc: "2026-08-16T23:59:59.000Z",
        assessmentsSubmittedCount: 0,
        averageScorePercent: null,
        totalAssessmentTimeSeconds: 0,
        modulesMarkedCompleteInWeek: 0,
      };
    });

    expect(data.students[0]?.displayName).toBe("Chipo");
    expect(data.students[0]?.badges).toHaveLength(1);
    expect(data.students[0]?.weeklySummary?.isoWeek).toBe(33);
  });
});
