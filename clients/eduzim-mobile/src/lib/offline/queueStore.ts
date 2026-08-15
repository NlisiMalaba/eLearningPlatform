import type { QueuedMutation } from "@/lib/offline/types";

export interface OfflineQueueStore {
  put(record: QueuedMutation): Promise<void>;
  getAll(): Promise<QueuedMutation[]>;
  deleteMany(ids: readonly string[]): Promise<void>;
  clear(): Promise<void>;
}
