import { describe, expect, it } from "vitest";
import { parseZimBotLanguage } from "@/lib/zimbot/languages";
import {
  isAssessmentPath,
  isLearningPath,
  readModuleIdFromPath,
} from "@/lib/zimbot/learningContext";
import { isZimBotUnavailable } from "@/lib/zimbot/unavailable";
import { ApiError, NetworkError } from "@/lib/api/errors";

describe("ZimBot language selection", () => {
  it("maps English, Shona, Ndebele, and Kalanga codes", () => {
    expect(parseZimBotLanguage("en")).toBe("English");
    expect(parseZimBotLanguage("Shona")).toBe("Shona");
    expect(parseZimBotLanguage("nd")).toBe("Ndebele");
    expect(parseZimBotLanguage("kck")).toBe("Kalanga");
  });
});

describe("learning context", () => {
  it("reads module ids and assessment flags from learning paths", () => {
    const moduleId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    expect(readModuleIdFromPath(`/modules/${moduleId}`)).toBe(moduleId);
    expect(isAssessmentPath("/modules/x/assessment")).toBe(true);
    expect(isLearningPath("/modules/abc")).toBe(true);
    expect(isLearningPath("/settings")).toBe(false);
  });
});

describe("ZimBot unavailable detection", () => {
  it("treats fallback replies, network errors, and 503 as unavailable", () => {
    expect(isZimBotUnavailable(null, true)).toBe(true);
    expect(isZimBotUnavailable(new NetworkError(), false)).toBe(true);
    expect(isZimBotUnavailable(new ApiError(503, "down"), false)).toBe(true);
    expect(isZimBotUnavailable(new ApiError(400, "bad"), false)).toBe(false);
  });
});
