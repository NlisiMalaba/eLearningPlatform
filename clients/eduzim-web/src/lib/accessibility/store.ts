import { create } from "zustand";
import { persist } from "zustand/middleware";
import { DEFAULT_FONT_SIZE, isValidFontSize, type FontSize } from "@/lib/accessibility/fontSize";
import { isValidTtsLanguage, type TtsLanguage } from "@/lib/accessibility/speech";

export const ACCESSIBILITY_STORAGE_KEY = "eduzim.accessibility";

type AccessibilityState = {
  highContrast: boolean;
  fontSize: FontSize;
  ttsLanguage: TtsLanguage;
  speaking: boolean;
  setHighContrast: (highContrast: boolean) => void;
  setFontSize: (fontSize: FontSize) => void;
  setTtsLanguage: (ttsLanguage: TtsLanguage) => void;
  setSpeaking: (speaking: boolean) => void;
};

type PersistedAccessibility = {
  highContrast?: unknown;
  fontSize?: unknown;
  ttsLanguage?: unknown;
};

export const useAccessibilityStore = create<AccessibilityState>()(
  persist(
    (set) => ({
      highContrast: false,
      fontSize: DEFAULT_FONT_SIZE,
      ttsLanguage: "en",
      speaking: false,
      setHighContrast: (highContrast) => set({ highContrast }),
      setFontSize: (fontSize) => {
        if (!isValidFontSize(fontSize)) {
          return;
        }

        set({ fontSize });
      },
      setTtsLanguage: (ttsLanguage) => {
        if (!isValidTtsLanguage(ttsLanguage)) {
          return;
        }

        set({ ttsLanguage });
      },
      setSpeaking: (speaking) => set({ speaking }),
    }),
    {
      name: ACCESSIBILITY_STORAGE_KEY,
      partialize: (state) => ({
        highContrast: state.highContrast,
        fontSize: state.fontSize,
        ttsLanguage: state.ttsLanguage,
      }),
      merge: (persisted, current) => mergePersisted(persisted, current),
    },
  ),
);

export function mergePersisted(
  persisted: unknown,
  current: AccessibilityState,
): AccessibilityState {
  const raw = unwrapPersisted(persisted);
  return {
    ...current,
    highContrast: raw.highContrast === true,
    fontSize: isValidFontSize(raw.fontSize) ? raw.fontSize : DEFAULT_FONT_SIZE,
    ttsLanguage: isValidTtsLanguage(raw.ttsLanguage) ? raw.ttsLanguage : "en",
  };
}

function unwrapPersisted(persisted: unknown): PersistedAccessibility {
  if (!persisted || typeof persisted !== "object") {
    return {};
  }

  if ("state" in persisted && persisted.state && typeof persisted.state === "object") {
    return persisted.state as PersistedAccessibility;
  }

  return persisted as PersistedAccessibility;
}
