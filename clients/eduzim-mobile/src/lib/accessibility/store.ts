import { useSyncExternalStore } from "react";
import {
  isValidTtsLanguage,
  type TtsLanguage,
} from "@/lib/accessibility/speech";

export const ACCESSIBILITY_STORAGE_KEY = "eduzim.accessibility";

export type AccessibilitySnapshot = {
  highContrast: boolean;
  ttsLanguage: TtsLanguage;
  speaking: boolean;
  settingsOpen: boolean;
};

type PersistedAccessibility = {
  highContrast?: unknown;
  ttsLanguage?: unknown;
};

const listeners = new Set<() => void>();

let snapshot: AccessibilitySnapshot = {
  highContrast: false,
  ttsLanguage: "en",
  speaking: false,
  settingsOpen: false,
};

function emit(): void {
  for (const listener of listeners) {
    listener();
  }
}

function setSnapshot(next: AccessibilitySnapshot): void {
  snapshot = next;
  emit();
}

export function setHighContrast(highContrast: boolean): void {
  setSnapshot({ ...snapshot, highContrast });
}

export function setTtsLanguage(ttsLanguage: TtsLanguage): void {
  if (!isValidTtsLanguage(ttsLanguage)) {
    return;
  }

  setSnapshot({ ...snapshot, ttsLanguage });
}

export function setSpeaking(speaking: boolean): void {
  setSnapshot({ ...snapshot, speaking });
}

export function setAccessibilitySettingsOpen(settingsOpen: boolean): void {
  setSnapshot({ ...snapshot, settingsOpen });
}

export function getAccessibilitySnapshot(): AccessibilitySnapshot {
  return snapshot;
}

export function subscribeAccessibility(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function useAccessibilityStore(): AccessibilitySnapshot {
  return useSyncExternalStore(subscribeAccessibility, getAccessibilitySnapshot, getAccessibilitySnapshot);
}

export function mergePersisted(
  persisted: unknown,
  current: AccessibilitySnapshot,
): AccessibilitySnapshot {
  const raw = unwrapPersisted(persisted);
  return {
    ...current,
    highContrast: raw.highContrast === true,
    ttsLanguage: isValidTtsLanguage(raw.ttsLanguage) ? raw.ttsLanguage : "en",
  };
}

export function toPersistedAccessibility(state: AccessibilitySnapshot): PersistedAccessibility {
  return {
    highContrast: state.highContrast,
    ttsLanguage: state.ttsLanguage,
  };
}

export function applyPersistedAccessibility(persisted: unknown): void {
  snapshot = mergePersisted(persisted, snapshot);
  emit();
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
