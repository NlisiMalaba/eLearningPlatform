import { describe, expect, it } from "vitest";
import { mapClassResults, toCreatePayload } from "@/lib/teacher/assessmentService";
import { createQuestionDraft } from "@/lib/teacher/assessmentDraft";

describe("assessmentService mapping", () => {
  it("maps class results including completion rate and time-on-task", () => {
    const mapped = mapClassResults({
      schoolClassId: "class-1",
      schoolClassName: "2A",
      enrolledStudentCount: 2,
      completedCount: 1,
      completionRatePercent: 50,
      averageTimeTakenSecondsAmongCompleters: 90,
      students: [
        { studentId: "s1", scorePercent: 80, hasCompleted: true, timeTakenSeconds: 90 },
        { studentId: "s2", scorePercent: null, hasCompleted: false, timeTakenSeconds: null },
      ],
    });

    expect(mapped.completionRatePercent).toBe(50);
    expect(mapped.averageTimeTakenSecondsAmongCompleters).toBe(90);
    expect(mapped.students[0]?.scorePercent).toBe(80);
    expect(mapped.students[1]?.hasCompleted).toBe(false);
  });

  it("sends question types as numeric enum values", () => {
    const payload = toCreatePayload({
      title: "Quiz",
      moduleId: "mod-1",
      passingScorePercent: 60,
      timed: false,
      timeLimitMinutes: 10,
      questions: [
        { ...createQuestionDraft("ShortAnswer"), text: "Name a fraction", correctShortAnswer: "1/2" },
      ],
    });
    const questions = payload.questions as Array<Record<string, unknown>>;
    expect(questions[0]?.type).toBe(2);
    expect(payload.timeLimitSeconds).toBeNull();
  });
});
