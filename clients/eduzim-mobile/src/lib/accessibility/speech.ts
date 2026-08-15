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

export function normalizeReadableText(text: string): string {
  return text.replace(/\s+/g, " ").trim();
}

const sections = new Map<string, string>();

export function setReadableSection(id: string, text: string): void {
  const normalized = normalizeReadableText(text);
  if (normalized.length === 0) {
    sections.delete(id);
    return;
  }

  sections.set(id, normalized);
}

export function clearReadableSection(id: string): void {
  sections.delete(id);
}

export function collectReadableText(): string {
  return [...sections.values()].join(" ").trim();
}

export type SpeechHandlers = {
  onEnd: () => void;
  onError: () => void;
};

export type SpeechEngine = {
  speak: (text: string, languageTag: string, handlers: SpeechHandlers) => void;
  stop: () => void | Promise<void>;
};

let engine: SpeechEngine | null = null;

export function configureSpeechEngine(next: SpeechEngine | null): void {
  engine = next;
}

export function speakText(text: string, language: TtsLanguage, handlers: SpeechHandlers): boolean {
  const spoken = normalizeReadableText(text);
  if (!engine || spoken.length === 0) {
    return false;
  }

  void engine.stop();
  engine.speak(spoken, TTS_LANG_TAGS[language], handlers);
  return true;
}

export function stopSpeaking(): void {
  void engine?.stop();
}
