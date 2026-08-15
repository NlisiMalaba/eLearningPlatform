import { createId } from "@/lib/ids";
import type { OfflineQueueStore } from "@/lib/offline/queueStore";
import type { OfflineSyncPayload, QueuedMutation } from "@/lib/offline/types";

let store: OfflineQueueStore | undefined;

export function configureOfflineQueue(next: OfflineQueueStore): void {
  store = next;
}

export function getOfflineQueueStore(): OfflineQueueStore {
  if (!store) {
    throw new Error("Offline queue store is not configured.");
  }

  return store;
}

export async function enqueueMutation(
  studentId: string,
  payload: OfflineSyncPayload,
  localTimestamp: Date = new Date(),
): Promise<QueuedMutation> {
  const id = createId();
  const isoTimestamp = localTimestamp.toISOString();
  const record: QueuedMutation = {
    id,
    clientId: id,
    studentId,
    localTimestamp: isoTimestamp,
    queuedAt: isoTimestamp,
    payload,
  };

  await getOfflineQueueStore().put(record);
  return record;
}

export async function getQueuedMutations(): Promise<QueuedMutation[]> {
  return getOfflineQueueStore().getAll();
}

export async function deleteQueuedMutations(ids: readonly string[]): Promise<void> {
  await getOfflineQueueStore().deleteMany(ids);
}

export async function clearQueuedMutations(): Promise<void> {
  await getOfflineQueueStore().clear();
}
