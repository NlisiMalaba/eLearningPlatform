import type { QueuedMutation } from "@/lib/offline/types";
import type { OfflineQueueStore } from "@/lib/offline/queueStore";

export function createMemoryQueueStore(
  initial: readonly QueuedMutation[] = [],
): OfflineQueueStore {
  const records = new Map<string, QueuedMutation>(initial.map((item) => [item.id, item]));

  return {
    async put(record) {
      records.set(record.id, record);
    },
    async getAll() {
      return [...records.values()];
    },
    async deleteMany(ids) {
      for (const id of ids) {
        records.delete(id);
      }
    },
    async clear() {
      records.clear();
    },
  };
}
