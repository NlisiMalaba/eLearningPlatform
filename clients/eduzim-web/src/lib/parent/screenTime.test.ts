import { describe, expect, it } from "vitest";
import { hoursToSeconds, parseHoursInput, secondsToHours, toLimitSeconds } from "@/lib/parent/screenTime";

describe("screen time limit conversion", () => {
  it("converts hours to seconds and back", () => {
    expect(hoursToSeconds(2)).toBe(7_200);
    expect(secondsToHours(3_600)).toBe(1);
    expect(secondsToHours(null)).toBeNull();
  });

  it("clears the limit when the control is disabled", () => {
    expect(toLimitSeconds(false, 2)).toBeNull();
  });

  it("rejects hours outside the daily range", () => {
    expect(toLimitSeconds(true, parseHoursInput("25"))).toBe("invalid");
    expect(toLimitSeconds(true, parseHoursInput("0"))).toBe("invalid");
    expect(toLimitSeconds(true, parseHoursInput("2"))).toBe(7_200);
  });
});
