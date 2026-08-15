import { useSyncExternalStore } from "react";

export type ConnectivitySnapshot = {
  online: boolean;
  pendingCount: number;
};

const listeners = new Set<() => void>();

let snapshot: ConnectivitySnapshot = {
  online: true,
  pendingCount: 0,
};

function emit(): void {
  for (const listener of listeners) {
    listener();
  }
}

export function setOnline(online: boolean): void {
  if (snapshot.online === online) {
    return;
  }

  snapshot = { ...snapshot, online };
  emit();
}

export function setPendingCount(count: number): void {
  if (snapshot.pendingCount === count) {
    return;
  }

  snapshot = { ...snapshot, pendingCount: count };
  emit();
}

export function getConnectivitySnapshot(): ConnectivitySnapshot {
  return snapshot;
}

export function subscribeConnectivityStore(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function useConnectivityStore(): ConnectivitySnapshot {
  return useSyncExternalStore(subscribeConnectivityStore, getConnectivitySnapshot, getConnectivitySnapshot);
}
