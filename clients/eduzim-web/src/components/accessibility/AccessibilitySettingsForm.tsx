"use client";

import { FONT_SIZES, type FontSize } from "@/lib/accessibility/fontSize";
import {
  collectReadableText,
  speakText,
  stopSpeaking,
  TTS_LANGUAGES,
  type TtsLanguage,
} from "@/lib/accessibility/speech";
import { useAccessibilityStore } from "@/lib/accessibility/store";
import { t } from "@/lib/i18n/t";

type AccessibilitySettingsFormProps = {
  headingId?: string;
};

export function AccessibilitySettingsForm({ headingId }: AccessibilitySettingsFormProps) {
  const highContrast = useAccessibilityStore((state) => state.highContrast);
  const fontSize = useAccessibilityStore((state) => state.fontSize);
  const ttsLanguage = useAccessibilityStore((state) => state.ttsLanguage);
  const speaking = useAccessibilityStore((state) => state.speaking);
  const setHighContrast = useAccessibilityStore((state) => state.setHighContrast);
  const setFontSize = useAccessibilityStore((state) => state.setFontSize);
  const setTtsLanguage = useAccessibilityStore((state) => state.setTtsLanguage);
  const setSpeaking = useAccessibilityStore((state) => state.setSpeaking);

  function onReadAloud(): void {
    const main = document.getElementById("main-content") ?? document.body;
    const started = speakText(collectReadableText(main), ttsLanguage, {
      onEnd: () => setSpeaking(false),
      onError: () => setSpeaking(false),
    });
    setSpeaking(started);
  }

  function onStop(): void {
    stopSpeaking();
    setSpeaking(false);
  }

  return (
    <div className="flex flex-col gap-6">
      {headingId ? (
        <h2 id={headingId} className="text-lg font-semibold text-[#0B6E4F]">
          {t("a11y.panel.title")}
        </h2>
      ) : null}

      <div className="flex items-center justify-between gap-4">
        <span id="high-contrast-label" className="text-sm font-medium">
          {t("a11y.contrast.label")}
        </span>
        <button
          type="button"
          role="switch"
          aria-checked={highContrast}
          aria-labelledby="high-contrast-label"
          onClick={() => setHighContrast(!highContrast)}
          className="rounded-lg border border-zinc-300 px-3 py-2 text-sm font-medium outline-none hover:bg-zinc-50 focus-visible:ring-2 focus-visible:ring-[#0B6E4F]"
        >
          {highContrast ? t("a11y.contrast.on") : t("a11y.contrast.off")}
        </button>
      </div>

      <fieldset className="flex flex-col gap-2 border-0 p-0">
        <legend className="text-sm font-medium">{t("a11y.fontSize.label")}</legend>
        <div className="flex flex-col gap-2">
          {FONT_SIZES.map((size) => (
            <label key={size} className="flex cursor-pointer items-center gap-2 text-sm">
              <input
                type="radio"
                name="fontSize"
                value={size}
                checked={fontSize === size}
                onChange={() => setFontSize(size)}
              />
              {t(fontSizeLabelKey(size))}
            </label>
          ))}
        </div>
      </fieldset>

      <div className="flex flex-col gap-2">
        <label htmlFor="tts-language" className="text-sm font-medium">
          {t("a11y.tts.language")}
        </label>
        <select
          id="tts-language"
          value={ttsLanguage}
          onChange={(event) => setTtsLanguage(event.target.value as TtsLanguage)}
          className="rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-[#0B6E4F]"
        >
          {TTS_LANGUAGES.map((language) => (
            <option key={language} value={language}>
              {t(ttsLanguageLabelKey(language))}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={onReadAloud}
          aria-label={t("a11y.tts.read")}
          className="rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-medium text-white outline-none hover:bg-[#095c42] focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-[#0B6E4F]"
        >
          {t("a11y.tts.read")}
        </button>
        <button
          type="button"
          onClick={onStop}
          disabled={!speaking}
          aria-label={t("a11y.tts.stop")}
          className="rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium outline-none hover:bg-zinc-50 focus-visible:ring-2 focus-visible:ring-[#0B6E4F] disabled:opacity-60"
        >
          {t("a11y.tts.stop")}
        </button>
      </div>
    </div>
  );
}

function fontSizeLabelKey(size: FontSize): "a11y.fontSize.small" | "a11y.fontSize.medium" | "a11y.fontSize.large" | "a11y.fontSize.extraLarge" {
  switch (size) {
    case "Small":
      return "a11y.fontSize.small";
    case "Medium":
      return "a11y.fontSize.medium";
    case "Large":
      return "a11y.fontSize.large";
    case "ExtraLarge":
      return "a11y.fontSize.extraLarge";
  }
}

function ttsLanguageLabelKey(language: TtsLanguage): "a11y.tts.en" | "a11y.tts.sn" | "a11y.tts.nd" {
  switch (language) {
    case "en":
      return "a11y.tts.en";
    case "sn":
      return "a11y.tts.sn";
    case "nd":
      return "a11y.tts.nd";
  }
}
