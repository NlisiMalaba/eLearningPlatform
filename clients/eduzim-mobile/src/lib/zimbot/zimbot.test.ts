import { describe, expect, it } from "vitest";
import { ApiError, NetworkError } from "@/lib/api/errors";
import { parseZimBotLanguage } from "@/lib/zimbot/languages";
import { isZimBotUnavailable } from "@/lib/zimbot/unavailable";

describe("ZimBot language selection", () => {
  it("maps English, Shona, Ndebele, and Kalanga codes", () => {
    expect(parseZimBotLanguage("en")).toBe("English");
    expect(parseZimBotLanguage("Shona")).toBe("Shona");
    expect(parseZimBotLanguage("nd")).toBe("Ndebele");
    expect(parseZimBotLanguage("kck")).toBe("Kalanga");
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
