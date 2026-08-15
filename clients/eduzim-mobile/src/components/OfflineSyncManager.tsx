import { useEffect } from "react";
import NetInfo from "@react-native-community/netinfo";
import { tryFlushOfflineQueue } from "@/lib/offline/flushQueue";
import { isNetInfoOnline, subscribeConnectivityRestore } from "@/lib/offline/syncOnReconnect";
import { setOnline, setPendingCount } from "@/lib/offline/connectivityStore";
import { getQueuedMutations } from "@/lib/offline/queue";

export function OfflineSyncManager() {
  useEffect(() => {
    let cancelled = false;

    const refreshPending = async () => {
      const items = await getQueuedMutations();
      if (!cancelled) {
        setPendingCount(items.length);
      }
    };

    const unsubscribe = subscribeConnectivityRestore(
      {
        getIsOnline: async () => isNetInfoOnline(await NetInfo.fetch()),
        subscribe: (listener) =>
          NetInfo.addEventListener((state) => listener(isNetInfoOnline(state))),
      },
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

    void refreshPending();

    return () => {
      cancelled = true;
      unsubscribe();
    };
  }, []);

  return null;
}
