import { describe, expect, it } from "vitest";
import { secondsUntil } from "@/lib/auth/sessionCookies";

describe("secondsUntil", () => {
  it("returns at least one second", () => {
    const now = Date.parse("2026-08-14T12:00:00.000Z");
    expect(secondsUntil(new Date("2026-08-14T11:59:00.000Z"), now)).toBe(1);
  });

  it("rounds remaining lifetime down to whole seconds", () => {
    const now = Date.parse("2026-08-14T12:00:00.000Z");
    expect(secondsUntil(new Date("2026-08-14T12:10:00.400Z"), now)).toBe(600);
  });
});
