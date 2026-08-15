export const TTS_LANGUAGES = ["en", "sn", "nd"] as const;

export type TtsLanguage = (typeof TTS_LANGUAGES)[number];

export const TTS_LANG_TAGS: Record<TtsLanguage, string> = {
  en: "en-ZW",
  sn: "sn",
  nd: "nd",
};

export function isValidTtsLanguage(value: unknown): value is TtsLanguage {
  return typeof value === "string" && (TTS_LANGUAGES as readonly string[]).includes(value);
}

export function collectReadableText(root: ParentNode): string {
  return (root.textContent ?? "").replace(/\s+/g, " ").trim();
}

export function speakText(
  text: string,
  language: TtsLanguage,
  handlers: { onEnd: () => void; onError: () => void },
): boolean {
  if (typeof window === "undefined" || !("speechSynthesis" in window) || text.length === 0) {
    return false;
  }

  window.speechSynthesis.cancel();
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.lang = TTS_LANG_TAGS[language];
  const voice = findVoice(language);
  if (voice) {
    utterance.voice = voice;
  }

  utterance.onend = handlers.onEnd;
  utterance.onerror = handlers.onError;
  window.speechSynthesis.speak(utterance);
  return true;
}

export function stopSpeaking(): void {
  if (typeof window === "undefined" || !("speechSynthesis" in window)) {
    return;
  }

  window.speechSynthesis.cancel();
}

function findVoice(language: TtsLanguage): SpeechSynthesisVoice | undefined {
  const tag = TTS_LANG_TAGS[language];
  const voices = window.speechSynthesis.getVoices();
  return (
    voices.find((voice) => voice.lang === tag) ??
    voices.find((voice) => voice.lang.toLowerCase().startsWith(language))
  );
}
