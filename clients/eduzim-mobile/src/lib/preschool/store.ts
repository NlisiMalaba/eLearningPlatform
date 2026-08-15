import { useSyncExternalStore } from "react";
import {
  DEFAULT_PRESCHOOL_LANGUAGE,
  isPreschoolLanguage,
  type PreschoolLanguage,
} from "@/lib/preschool/languages";

export const PRESCHOOL_STORAGE_KEY = "eduzim.preschool";

export type PreschoolSnapshot = {
  language: PreschoolLanguage;
  stars: number;
};

type Persisted = {
  language?: unknown;
  stars?: unknown;
};

const listeners = new Set<() => void>();

let snapshot: PreschoolSnapshot = {
  language: DEFAULT_PRESCHOOL_LANGUAGE,
  stars: 0,
};

function emit(): void {
  for (const listener of listeners) {
    listener();
  }
}

function setSnapshot(next: PreschoolSnapshot): void {
  snapshot = next;
  emit();
}

export function setPreschoolLanguage(language: PreschoolLanguage): void {
  if (!isPreschoolLanguage(language)) {
    return;
  }

  setSnapshot({ ...snapshot, language });
}

export function awardPreschoolStar(): void {
  setSnapshot({ ...snapshot, stars: snapshot.stars + 1 });
}

export function getPreschoolSnapshot(): PreschoolSnapshot {
  return snapshot;
}

export function subscribePreschool(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function usePreschoolStore(): PreschoolSnapshot {
  return useSyncExternalStore(subscribePreschool, getPreschoolSnapshot, getPreschoolSnapshot);
}

export function mergePersistedPreschool(persisted: unknown, current: PreschoolSnapshot): PreschoolSnapshot {
  if (!persisted || typeof persisted !== "object") {
    return current;
  }

  const raw = persisted as Persisted;
  return {
    language: isPreschoolLanguage(raw.language) ? raw.language : current.language,
    stars: typeof raw.stars === "number" && Number.isFinite(raw.stars) && raw.stars >= 0 ? Math.floor(raw.stars) : current.stars,
  };
}

export function applyPersistedPreschool(persisted: unknown): void {
  snapshot = mergePersistedPreschool(persisted, snapshot);
  emit();
}

export function toPersistedPreschool(state: PreschoolSnapshot): Persisted {
  return { language: state.language, stars: state.stars };
}
