import { useSyncExternalStore } from "react";
import { AccessibilityInfo, findNodeHandle } from "react-native";
import { nextIndex, previousIndex, type SwitchAction } from "@/lib/accessibility/switchKeys";

export type SwitchTarget = {
  id: string;
  activate: () => void;
  getNode: () => Parameters<typeof findNodeHandle>[0];
};

type SwitchSnapshot = {
  focusedId: string | null;
};

const targets: SwitchTarget[] = [];
const listeners = new Set<() => void>();
let snapshot: SwitchSnapshot = { focusedId: null };

function emit(): void {
  for (const listener of listeners) {
    listener();
  }
}

function setFocusedId(focusedId: string | null): void {
  if (snapshot.focusedId === focusedId) {
    return;
  }

  snapshot = { focusedId };
  emit();
}

export function registerSwitchTarget(target: SwitchTarget): () => void {
  targets.push(target);
  return () => {
    const index = targets.findIndex((item) => item.id === target.id);
    if (index >= 0) {
      targets.splice(index, 1);
    }

    if (snapshot.focusedId === target.id) {
      setFocusedId(targets[0]?.id ?? null);
    }
  };
}

export function applySwitchAction(action: SwitchAction): void {
  if (targets.length === 0) {
    setFocusedId(null);
    return;
  }

  const current = targets.findIndex((item) => item.id === snapshot.focusedId);
  if (action === "activate") {
    const target = current >= 0 ? targets[current] : targets[0];
    if (target) {
      setFocusedId(target.id);
      target.activate();
      focusNative(target);
    }
    return;
  }

  const index = action === "next" ? nextIndex(current, targets.length) : previousIndex(current, targets.length);
  const target = targets[index];
  if (!target) {
    return;
  }

  setFocusedId(target.id);
  focusNative(target);
}

export function getSwitchAccessSnapshot(): SwitchSnapshot {
  return snapshot;
}

export function subscribeSwitchAccess(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function useSwitchAccess(): SwitchSnapshot {
  return useSyncExternalStore(subscribeSwitchAccess, getSwitchAccessSnapshot, getSwitchAccessSnapshot);
}

export function resetSwitchTargetsForTests(): void {
  targets.splice(0, targets.length);
  snapshot = { focusedId: null };
}

function focusNative(target: SwitchTarget): void {
  try {
    const node = target.getNode();
    const tag = typeof node === "number" ? node : findNodeHandle(node);
    if (typeof tag === "number") {
      AccessibilityInfo.setAccessibilityFocus(tag);
    }
  } catch {
    // Native focus is unavailable in unit tests and some simulators.
  }
}
