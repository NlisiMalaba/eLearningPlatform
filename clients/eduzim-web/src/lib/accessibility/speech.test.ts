import { describe, expect, it } from "vitest";
import { collectReadableText, isValidTtsLanguage, TTS_LANG_TAGS } from "@/lib/accessibility/speech";

describe("text-to-speech helpers", () => {
  it("maps English, Shona, and Ndebele to speech tags", () => {
    expect(TTS_LANG_TAGS.en).toBe("en-ZW");
    expect(TTS_LANG_TAGS.sn).toBe("sn");
    expect(TTS_LANG_TAGS.nd).toBe("nd");
    expect(isValidTtsLanguage("sn")).toBe(true);
    expect(isValidTtsLanguage("fr")).toBe(false);
  });

  it("normalizes on-screen text for speech", () => {
    const root = document.createElement("main");
    root.textContent = "  Learn\n  well  ";
    expect(collectReadableText(root)).toBe("Learn well");
  });
});
