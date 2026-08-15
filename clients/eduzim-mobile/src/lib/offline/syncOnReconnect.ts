export type ConnectivityListener = (online: boolean) => void;

export type ConnectivitySource = {
  getIsOnline: () => boolean | Promise<boolean>;
  subscribe: (listener: ConnectivityListener) => () => void;
};

export function subscribeConnectivityRestore(
  source: ConnectivitySource,
  onOnline: () => void | Promise<void>,
  onOffline?: () => void,
): () => void {
  let lastOnline: boolean | undefined;

  const handle = async (online: boolean): Promise<void> => {
    const previous = lastOnline;
    lastOnline = online;
    if (online) {
      if (previous === false || previous === undefined) {
        await onOnline();
      }
      return;
    }

    onOffline?.();
  };

  const unsubscribe = source.subscribe((online) => {
    void handle(online);
  });

  void Promise.resolve(source.getIsOnline()).then((online) => {
    void handle(online);
  });

  return unsubscribe;
}

export function isNetInfoOnline(state: {
  isConnected?: boolean | null;
  isInternetReachable?: boolean | null;
}): boolean {
  if (state.isConnected !== true) {
    return false;
  }

  return state.isInternetReachable !== false;
}

export function createNavigatorOnlineSource(): ConnectivitySource {
  return {
    getIsOnline: () => typeof navigator !== "undefined" && navigator.onLine === true,
    subscribe: (listener) => {
      if (typeof globalThis.addEventListener !== "function") {
        return () => undefined;
      }

      const onOnline = () => listener(true);
      const onOffline = () => listener(false);
      globalThis.addEventListener("online", onOnline);
      globalThis.addEventListener("offline", onOffline);
      return () => {
        globalThis.removeEventListener("online", onOnline);
        globalThis.removeEventListener("offline", onOffline);
      };
    },
  };
}

export function combineConnectivitySources(sources: ConnectivitySource[]): ConnectivitySource {
  return {
    getIsOnline: async () => {
      const flags = await Promise.all(sources.map((source) => source.getIsOnline()));
      return flags.some((online) => online);
    },
    subscribe: (listener) => {
      const unsubscribes = sources.map((source) => source.subscribe(listener));
      return () => {
        for (const unsubscribe of unsubscribes) {
          unsubscribe();
        }
      };
    },
  };
}
