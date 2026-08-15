import type { MessageKey } from "@/lib/i18n/messages";
import type { GradeLevel } from "@/lib/teacher/grades";

export const GRADE_MESSAGE_KEYS: Record<GradeLevel, MessageKey> = {
  EcdGrade0: "teacher.grade.EcdGrade0",
  EcdGrade1: "teacher.grade.EcdGrade1",
  Grade1: "teacher.grade.Grade1",
  Grade2: "teacher.grade.Grade2",
  Grade3: "teacher.grade.Grade3",
  Grade4: "teacher.grade.Grade4",
  Grade5: "teacher.grade.Grade5",
  Grade6: "teacher.grade.Grade6",
  Grade7: "teacher.grade.Grade7",
};
