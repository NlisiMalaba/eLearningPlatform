import { apiFetch } from "@/lib/api/client";
import type {
  QueuedMutation,
  UploadOfflineQueueRequest,
  UploadOfflineQueueResponse,
} from "@/lib/offline/types";

export function toUploadRequest(
  studentId: string,
  items: readonly QueuedMutation[],
): UploadOfflineQueueRequest {
  return {
    studentId,
    items: items.map((item) => ({
      clientId: item.clientId,
      localTimestamp: item.localTimestamp,
      payload: item.payload,
    })),
  };
}

export function groupByStudentId(
  items: readonly QueuedMutation[],
): Map<string, QueuedMutation[]> {
  const groups = new Map<string, QueuedMutation[]>();

  for (const item of items) {
    const existing = groups.get(item.studentId);
    if (existing) {
      existing.push(item);
      continue;
    }

    groups.set(item.studentId, [item]);
  }

  return groups;
}

export async function uploadOfflineQueue(
  studentId: string,
  items: readonly QueuedMutation[],
): Promise<UploadOfflineQueueResponse> {
  return apiFetch<UploadOfflineQueueResponse>("/api/v1/sync/upload", {
    method: "POST",
    body: JSON.stringify(toUploadRequest(studentId, items)),
  });
}
