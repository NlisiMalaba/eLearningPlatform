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
