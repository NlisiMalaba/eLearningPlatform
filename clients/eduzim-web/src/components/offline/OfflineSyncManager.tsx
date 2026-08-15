"use client";

import { useEffect } from "react";
import { getQueuedMutations } from "@/lib/offline/queue";
import { tryFlushOfflineQueue } from "@/lib/offline/flushQueue";
import { useConnectivityStore } from "@/lib/offline/connectivityStore";
import {
  createNavigatorOnlineSource,
  subscribeConnectivityRestore,
} from "@/lib/offline/syncOnReconnect";

export function OfflineSyncManager() {
  const setOnline = useConnectivityStore((state) => state.setOnline);
  const setPendingCount = useConnectivityStore((state) => state.setPendingCount);

  useEffect(() => {
    let cancelled = false;

    const refreshPendingCount = async () => {
      try {
        const items = await getQueuedMutations();
        if (!cancelled) {
          setPendingCount(items.length);
        }
      } catch {
        // IndexedDB may be unavailable in private browsing; keep the last known count.
      }
    };

    const unsubscribe = subscribeConnectivityRestore(
      createNavigatorOnlineSource(),
      async () => {
        setOnline(true);
        const result = await tryFlushOfflineQueue();
        if (!cancelled) {
          setPendingCount(result.remaining);
        }
      },
      () => {
        setOnline(false);
      },
    );

    void refreshPendingCount();

    return () => {
      cancelled = true;
      unsubscribe();
    };
  }, [setOnline, setPendingCount]);

  return null;
}
