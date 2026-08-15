import { useSyncExternalStore } from "react";
import {
  DEFAULT_ZIMBOT_LANGUAGE,
  isZimBotLanguage,
  type ZimBotLanguage,
} from "@/lib/zimbot/languages";
import type { ChatMessage } from "@/lib/zimbot/types";

export type ZimBotSnapshot = {
  open: boolean;
  helpOpen: boolean;
  language: ZimBotLanguage;
  messages: ChatMessage[];
  sending: boolean;
};

const listeners = new Set<() => void>();

let snapshot: ZimBotSnapshot = {
  open: false,
  helpOpen: false,
  language: DEFAULT_ZIMBOT_LANGUAGE,
  messages: [],
  sending: false,
};

function emit(): void {
  for (const listener of listeners) {
    listener();
  }
}

function setSnapshot(next: ZimBotSnapshot): void {
  snapshot = next;
  emit();
}

export function setZimBotOpen(open: boolean): void {
  setSnapshot({ ...snapshot, open, helpOpen: open ? snapshot.helpOpen : false });
}

export function setZimBotHelpOpen(helpOpen: boolean): void {
  setSnapshot({ ...snapshot, helpOpen });
}

export function setZimBotLanguage(language: ZimBotLanguage): void {
  if (!isZimBotLanguage(language)) {
    return;
  }

  setSnapshot({ ...snapshot, language });
}

export function appendZimBotMessage(message: ChatMessage): void {
  setSnapshot({
    ...snapshot,
    messages: [...snapshot.messages, message].slice(-50),
  });
}

export function setZimBotSending(sending: boolean): void {
  setSnapshot({ ...snapshot, sending });
}

export function getZimBotSnapshot(): ZimBotSnapshot {
  return snapshot;
}

export function subscribeZimBotStore(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function useZimBotStore(): ZimBotSnapshot {
  return useSyncExternalStore(subscribeZimBotStore, getZimBotSnapshot, getZimBotSnapshot);
}
