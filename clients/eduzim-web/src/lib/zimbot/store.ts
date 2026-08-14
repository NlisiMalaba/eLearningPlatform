import { create } from "zustand";
import { persist } from "zustand/middleware";
import {
  DEFAULT_ZIMBOT_LANGUAGE,
  isZimBotLanguage,
  type ZimBotLanguage,
} from "@/lib/zimbot/languages";
import type { ChatMessage } from "@/lib/zimbot/types";

export const ZIMBOT_STORAGE_KEY = "eduzim.zimbot";

type ZimBotState = {
  open: boolean;
  language: ZimBotLanguage;
  messages: ChatMessage[];
  sending: boolean;
  setOpen: (open: boolean) => void;
  setLanguage: (language: ZimBotLanguage) => void;
  appendMessage: (message: ChatMessage) => void;
  setSending: (sending: boolean) => void;
};

type PersistedZimBot = {
  language?: unknown;
  messages?: unknown;
};

export const useZimBotStore = create<ZimBotState>()(
  persist(
    (set) => ({
      open: false,
      language: DEFAULT_ZIMBOT_LANGUAGE,
      messages: [],
      sending: false,
      setOpen: (open) => set({ open }),
      setLanguage: (language) => {
        if (!isZimBotLanguage(language)) {
          return;
        }

        set({ language });
      },
      appendMessage: (message) =>
        set((state) => ({ messages: [...state.messages, message].slice(-50) })),
      setSending: (sending) => set({ sending }),
    }),
    {
      name: ZIMBOT_STORAGE_KEY,
      partialize: (state) => ({ language: state.language, messages: state.messages }),
      merge: (persisted, current) => mergePersisted(persisted, current),
    },
  ),
);

export function mergePersisted(persisted: unknown, current: ZimBotState): ZimBotState {
  const raw = unwrap(persisted);
  return {
    ...current,
    language: isZimBotLanguage(raw.language) ? raw.language : DEFAULT_ZIMBOT_LANGUAGE,
    messages: parseMessages(raw.messages),
  };
}

function parseMessages(value: unknown): ChatMessage[] {
  if (!Array.isArray(value)) {
    return [];
  }

  return value.flatMap((item) => {
    if (!item || typeof item !== "object") {
      return [];
    }

    const row = item as Record<string, unknown>;
    const id = typeof row.id === "string" ? row.id : "";
    const text = typeof row.text === "string" ? row.text : "";
    const role = row.role === "student" || row.role === "zimbot" ? row.role : null;
    if (!id || !text || !role) {
      return [];
    }

    return [
      {
        id,
        role,
        text,
        usedFallback: row.usedFallback === true,
        isLowConfidence: row.isLowConfidence === true,
      },
    ];
  });
}

function unwrap(persisted: unknown): PersistedZimBot {
  if (!persisted || typeof persisted !== "object") {
    return {};
  }

  if ("state" in persisted && persisted.state && typeof persisted.state === "object") {
    return persisted.state as PersistedZimBot;
  }

  return persisted as PersistedZimBot;
}
