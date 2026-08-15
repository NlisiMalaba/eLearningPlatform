import { describe, expect, it } from "vitest";
import { itemsForCategory, preschoolCatalog } from "@/lib/preschool/catalog";
import { isPreschoolLanguage } from "@/lib/preschool/languages";
import { preschoolT } from "@/lib/preschool/messages";
import {
  acknowledgePreschoolRest,
  createPreschoolSessionClock,
  evaluatePreschoolSession,
  INACTIVITY_PAUSE_AFTER_MS,
  recordPreschoolInteraction,
  REST_PROMPT_AFTER_MS,
  resumePreschoolSession,
  shouldPauseForInactivity,
  shouldPromptRest,
} from "@/lib/preschool/session";
import { mergePersistedPreschool } from "@/lib/preschool/store";

describe("preschool session timers", () => {
  it("prompts rest after 20 minutes of continuous interaction", () => {
    const start = 1_000_000;
    const clock = createPreschoolSessionClock(start);
    expect(shouldPromptRest(clock, start + REST_PROMPT_AFTER_MS - 1)).toBe(false);
    expect(shouldPromptRest(clock, start + REST_PROMPT_AFTER_MS)).toBe(true);
    const prompted = evaluatePreschoolSession(clock, start + REST_PROMPT_AFTER_MS);
    expect(prompted.restPromptRequired).toBe(true);
  });

  it("pauses when idle for 60 seconds", () => {
    const start = 5_000_000;
    let clock = createPreschoolSessionClock(start);
    clock = recordPreschoolInteraction(clock, start);
    expect(shouldPauseForInactivity(clock, start + INACTIVITY_PAUSE_AFTER_MS - 1)).toBe(false);
    const paused = evaluatePreschoolSession(clock, start + INACTIVITY_PAUSE_AFTER_MS);
    expect(paused.paused).toBe(true);
  });

  it("resets the segment after rest or resume", () => {
    const now = 9_000_000;
    const rested = acknowledgePreschoolRest(createPreschoolSessionClock(now - REST_PROMPT_AFTER_MS), now);
    expect(rested.restPromptRequired).toBe(false);
    expect(rested.segmentStartedAt).toBe(now);
    const resumed = resumePreschoolSession({ ...createPreschoolSessionClock(now), paused: true }, now + 10);
    expect(resumed.paused).toBe(false);
    expect(resumed.lastInteractionAt).toBe(now + 10);
  });
});

describe("preschool catalog", () => {
  it("covers alphabet, numbers 1-100, and foundational categories", () => {
    const catalog = preschoolCatalog();
    expect(itemsForCategory("alphabet")).toHaveLength(26);
    expect(itemsForCategory("numbers")).toHaveLength(100);
    expect(catalog.some((item) => item.category === "shapes")).toBe(true);
    expect(catalog.some((item) => item.category === "colours")).toBe(true);
    expect(catalog.some((item) => item.category === "animals")).toBe(true);
    expect(catalog.some((item) => item.category === "body")).toBe(true);
    expect(catalog.some((item) => item.category === "vernacular")).toBe(true);
  });
});

describe("preschool language", () => {
  it("accepts English, Shona, and Ndebele and translates chrome", () => {
    expect(isPreschoolLanguage("en")).toBe(true);
    expect(isPreschoolLanguage("sn")).toBe(true);
    expect(isPreschoolLanguage("nd")).toBe(true);
    expect(preschoolT("title", "sn")).toContain("dzidza");
    expect(mergePersistedPreschool({ language: "nd", stars: 3 }, { language: "en", stars: 0 })).toEqual({
      language: "nd",
      stars: 3,
    });
    expect(mergePersistedPreschool({ language: "xx" }, { language: "en", stars: 0 }).language).toBe("en");
  });
});
