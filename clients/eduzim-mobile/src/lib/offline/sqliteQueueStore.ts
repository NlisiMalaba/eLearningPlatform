import * as SQLite from "expo-sqlite";
import type { QueuedMutation } from "@/lib/offline/types";
import type { OfflineQueueStore } from "@/lib/offline/queueStore";

const DATABASE_NAME = "eduzim-offline.db";
const TABLE = "offline_sync_queue";

type QueueRow = {
  id: string;
  client_id: string;
  student_id: string;
  local_timestamp: string;
  queued_at: string;
  payload: string;
};

export async function createSqliteQueueStore(): Promise<OfflineQueueStore> {
  const db = await SQLite.openDatabaseAsync(DATABASE_NAME);
  await db.execAsync(
    `CREATE TABLE IF NOT EXISTS ${TABLE} (
      id TEXT PRIMARY KEY NOT NULL,
      client_id TEXT NOT NULL,
      student_id TEXT NOT NULL,
      local_timestamp TEXT NOT NULL,
      queued_at TEXT NOT NULL,
      payload TEXT NOT NULL
    );`,
  );

  return {
    async put(record) {
      await db.runAsync(
        `INSERT OR REPLACE INTO ${TABLE}
          (id, client_id, student_id, local_timestamp, queued_at, payload)
         VALUES (?, ?, ?, ?, ?, ?);`,
        [
          record.id,
          record.clientId,
          record.studentId,
          record.localTimestamp,
          record.queuedAt,
          JSON.stringify(record.payload),
        ],
      );
    },
    async getAll() {
      const rows = await db.getAllAsync<QueueRow>(
        `SELECT id, client_id, student_id, local_timestamp, queued_at, payload FROM ${TABLE};`,
      );
      return rows.map(mapRow);
    },
    async deleteMany(ids) {
      if (ids.length === 0) {
        return;
      }

      const placeholders = ids.map(() => "?").join(", ");
      await db.runAsync(`DELETE FROM ${TABLE} WHERE id IN (${placeholders});`, [...ids]);
    },
    async clear() {
      await db.execAsync(`DELETE FROM ${TABLE};`);
    },
  };
}

function mapRow(row: QueueRow): QueuedMutation {
  return {
    id: row.id,
    clientId: row.client_id,
    studentId: row.student_id,
    localTimestamp: row.local_timestamp,
    queuedAt: row.queued_at,
    payload: JSON.parse(row.payload) as QueuedMutation["payload"],
  };
}
