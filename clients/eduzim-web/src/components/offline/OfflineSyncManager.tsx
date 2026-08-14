"use client";

import { useEffect } from "react";
import { getQueuedMutations } from "@/lib/offline/queue";
import { tryFlushOfflineQueue } from "@/lib/offline/flushQueue";
import { useConnectivityStore } from "@/lib/offline/connectivityStore";

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

    const handleOnline = async () => {
      setOnline(true);
      const result = await tryFlushOfflineQueue();
      if (!cancelled) {
        setPendingCount(result.remaining);
      }
    };

    const handleOffline = () => {
      setOnline(false);
    };

    void refreshPendingCount();

    if (navigator.onLine) {
      void handleOnline();
    } else {
      setOnline(false);
    }

    window.addEventListener("online", handleOnline);
    window.addEventListener("offline", handleOffline);

    return () => {
      cancelled = true;
      window.removeEventListener("online", handleOnline);
      window.removeEventListener("offline", handleOffline);
    };
  }, [setOnline, setPendingCount]);

  return null;
}
