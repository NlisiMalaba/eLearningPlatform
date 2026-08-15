import { describe, expect, it } from "vitest";
import { mergePersisted } from "@/lib/accessibility/store";
import {
  collectReadableText,
  configureSpeechEngine,
  isValidTtsLanguage,
  normalizeReadableText,
  setReadableSection,
  speakText,
  TTS_LANG_TAGS,
} from "@/lib/accessibility/speech";
import {
  interpretSwitchKey,
  nextIndex,
  previousIndex,
} from "@/lib/accessibility/switchKeys";
import { paletteForContrast } from "@/theme";

describe("text-to-speech helpers", () => {
  it("maps English, Shona, and Ndebele to speech tags", () => {
    expect(TTS_LANG_TAGS.en).toBe("en-ZW");
    expect(TTS_LANG_TAGS.sn).toBe("sn");
    expect(TTS_LANG_TAGS.nd).toBe("nd");
    expect(isValidTtsLanguage("sn")).toBe(true);
    expect(isValidTtsLanguage("fr")).toBe(false);
  });

  it("normalizes and collects registered on-screen text", () => {
    setReadableSection("a", "  Learn\n  well  ");
    setReadableSection("b", "Grade 3");
    expect(normalizeReadableText("  Learn\n  well  ")).toBe("Learn well");
    expect(collectReadableText()).toContain("Learn well");
    expect(collectReadableText()).toContain("Grade 3");
  });

  it("speaks through the configured engine", () => {
    const spoken: string[] = [];
    configureSpeechEngine({
      speak: (text) => {
        spoken.push(text);
      },
      stop: () => undefined,
    });
    expect(speakText("Hello Harare", "en", { onEnd: () => undefined, onError: () => undefined })).toBe(true);
    expect(spoken).toEqual(["Hello Harare"]);
    configureSpeechEngine(null);
    expect(speakText("Hello", "en", { onEnd: () => undefined, onError: () => undefined })).toBe(false);
  });
});

describe("accessibility preference merge", () => {
  it("keeps high contrast and falls back for invalid TTS language", () => {
    const merged = mergePersisted(
      { state: { highContrast: true, ttsLanguage: "xx" } },
      {
        highContrast: false,
        ttsLanguage: "en",
        speaking: false,
        settingsOpen: false,
      },
    );
    expect(merged.highContrast).toBe(true);
    expect(merged.ttsLanguage).toBe("en");
  });
});

describe("switch access keys", () => {
  it("maps tab, arrows, and activate keys used by switch hardware", () => {
    expect(interpretSwitchKey("Tab")).toBe("next");
    expect(interpretSwitchKey("Tab", true)).toBe("prev");
    expect(interpretSwitchKey("ArrowDown")).toBe("next");
    expect(interpretSwitchKey("Enter")).toBe("activate");
    expect(interpretSwitchKey(" ")).toBe("activate");
    expect(interpretSwitchKey("Select")).toBe("activate");
    expect(nextIndex(0, 3)).toBe(1);
    expect(previousIndex(0, 3)).toBe(2);
  });
});

describe("high-contrast palette", () => {
  it("uses a black background and light text when high contrast is on", () => {
    const palette = paletteForContrast(true);
    expect(palette.background).toBe("#000000");
    expect(palette.text).toBe("#FFFFFF");
    expect(palette.border).toBe("#FFFFFF");
  });
});
