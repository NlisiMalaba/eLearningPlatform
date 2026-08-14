import { describe, expect, it } from "vitest";
import { canManageSchoolContent } from "@/lib/teacher/roles";
import { isLearningPath } from "@/lib/zimbot/learningContext";

describe("canManageSchoolContent", () => {
  it("allows teachers and school admins only", () => {
    expect(canManageSchoolContent("Teacher")).toBe(true);
    expect(canManageSchoolContent("SchoolAdmin")).toBe(true);
    expect(canManageSchoolContent("PlatformAdmin")).toBe(true);
    expect(canManageSchoolContent("Student")).toBe(false);
    expect(canManageSchoolContent("ParentGuardian")).toBe(false);
  });
});

describe("teacher studio is not a ZimBot learning path", () => {
  it("keeps the chat widget off teacher screens", () => {
    expect(isLearningPath("/teacher")).toBe(false);
    expect(isLearningPath("/teacher/content")).toBe(false);
    expect(isLearningPath("/teacher/modules")).toBe(false);
  });
});
