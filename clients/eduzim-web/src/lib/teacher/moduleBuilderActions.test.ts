import { describe, expect, it } from "vitest";
import { moveAttachedId, toggleAttachedId } from "@/lib/teacher/moduleBuilderActions";

describe("module builder order", () => {
  it("adds, removes, and reorders content item ids", () => {
    expect(toggleAttachedId(["a"], "b")).toEqual(["a", "b"]);
    expect(toggleAttachedId(["a", "b"], "a")).toEqual(["b"]);
    expect(moveAttachedId(["a", "b", "c"], "c", -1)).toEqual(["a", "c", "b"]);
    expect(moveAttachedId(["a", "b"], "a", -1)).toEqual(["a", "b"]);
  });
});
