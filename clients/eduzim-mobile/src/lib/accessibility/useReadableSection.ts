import { useCallback, useEffect } from "react";
import {
  clearReadableSection,
  collectReadableText,
  setReadableSection,
  speakText,
  stopSpeaking,
} from "@/lib/accessibility/speech";
import { getAccessibilitySnapshot, setSpeaking } from "@/lib/accessibility/store";

export function useReadableSection(id: string, text: string): void {
  useEffect(() => {
    setReadableSection(id, text);
    return () => clearReadableSection(id);
  }, [id, text]);
}

export function readCurrentScreenAloud(): boolean {
  const { ttsLanguage } = getAccessibilitySnapshot();
  const started = speakText(collectReadableText(), ttsLanguage, {
    onEnd: () => setSpeaking(false),
    onError: () => setSpeaking(false),
  });
  setSpeaking(started);
  return started;
}

export function stopReadingAloud(): void {
  stopSpeaking();
  setSpeaking(false);
}

export function useReadAloud(): { readAloud: () => void; stop: () => void } {
  const readAloud = useCallback(() => {
    readCurrentScreenAloud();
  }, []);
  const stop = useCallback(() => {
    stopReadingAloud();
  }, []);
  return { readAloud, stop };
}
