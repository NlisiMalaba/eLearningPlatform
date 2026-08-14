import { describe, expect, it } from "vitest";
import { DEFAULT_FONT_SIZE } from "@/lib/accessibility/fontSize";
import { mergePersisted, useAccessibilityStore } from "@/lib/accessibility/store";

describe("accessibility preference merge", () => {
  it("falls back to Medium when a stored font size is invalid", () => {
    const merged = mergePersisted(
      { state: { highContrast: true, fontSize: "huge", ttsLanguage: "xx" } },
      useAccessibilityStore.getState(),
    );

    expect(merged.highContrast).toBe(true);
    expect(merged.fontSize).toBe(DEFAULT_FONT_SIZE);
    expect(merged.ttsLanguage).toBe("en");
  });

  it("keeps valid persisted font size and TTS language", () => {
    const merged = mergePersisted(
      { highContrast: false, fontSize: "ExtraLarge", ttsLanguage: "nd" },
      useAccessibilityStore.getState(),
    );

    expect(merged.fontSize).toBe("ExtraLarge");
    expect(merged.ttsLanguage).toBe("nd");
  });
});
