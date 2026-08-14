import { create } from "zustand";

type ConnectivityState = {
  isOnline: boolean;
  pendingCount: number;
  setOnline: (isOnline: boolean) => void;
  setPendingCount: (pendingCount: number) => void;
};

export const useConnectivityStore = create<ConnectivityState>((set) => ({
  isOnline: true,
  pendingCount: 0,
  setOnline: (isOnline) => set({ isOnline }),
  setPendingCount: (pendingCount) => set({ pendingCount }),
}));
