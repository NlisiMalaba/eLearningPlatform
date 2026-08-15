import { openDB, type DBSchema, type IDBPDatabase } from "idb";
import type { OfflineSyncPayload, QueuedMutation } from "@/lib/offline/types";

const DB_NAME = "eduzim-offline";
const STORE_NAME = "mutations";
const DB_VERSION = 1;

interface OfflineQueueSchema extends DBSchema {
  mutations: {
    key: string;
    value: QueuedMutation;
    indexes: { "by-student": string };
  };
}

let dbPromise: Promise<IDBPDatabase<OfflineQueueSchema>> | undefined;

function getDb(): Promise<IDBPDatabase<OfflineQueueSchema>> {
  if (typeof indexedDB === "undefined") {
    return Promise.reject(new Error("IndexedDB is not available"));
  }

  dbPromise ??= openDB<OfflineQueueSchema>(DB_NAME, DB_VERSION, {
    upgrade(database) {
      if (!database.objectStoreNames.contains(STORE_NAME)) {
        const store = database.createObjectStore(STORE_NAME, { keyPath: "id" });
        store.createIndex("by-student", "studentId");
      }
    },
  });

  return dbPromise;
}

export async function enqueueMutation(
  studentId: string,
  payload: OfflineSyncPayload,
  localTimestamp: Date = new Date(),
): Promise<QueuedMutation> {
  const id = crypto.randomUUID();
  const isoTimestamp = localTimestamp.toISOString();
  const record: QueuedMutation = {
    id,
    clientId: id,
    studentId,
    localTimestamp: isoTimestamp,
    queuedAt: isoTimestamp,
    payload,
  };

  const db = await getDb();
  await db.put(STORE_NAME, record);
  return record;
}

export async function getQueuedMutations(): Promise<QueuedMutation[]> {
  const db = await getDb();
  return db.getAll(STORE_NAME);
}

export async function deleteQueuedMutations(ids: readonly string[]): Promise<void> {
  if (ids.length === 0) {
    return;
  }

  const db = await getDb();
  const tx = db.transaction(STORE_NAME, "readwrite");
  await Promise.all(ids.map((id) => tx.store.delete(id)));
  await tx.done;
}

export async function clearQueuedMutations(): Promise<void> {
  const db = await getDb();
  await db.clear(STORE_NAME);
}
