import { groupByStudentId, uploadOfflineQueue } from "@/lib/api/syncService";
import { deleteQueuedMutations, getQueuedMutations } from "@/lib/offline/queue";

export type FlushResult = {
  uploaded: number;
  remaining: number;
};

export async function flushOfflineQueue(): Promise<FlushResult> {
  const items = await getQueuedMutations();
  if (items.length === 0) {
    return { uploaded: 0, remaining: 0 };
  }

  let uploaded = 0;
  const groups = groupByStudentId(items);

  for (const [studentId, group] of groups) {
    await uploadOfflineQueue(studentId, group);
    await deleteQueuedMutations(group.map((item) => item.id));
    uploaded += group.length;
  }

  const remaining = await getQueuedMutations();
  return { uploaded, remaining: remaining.length };
}

export async function tryFlushOfflineQueue(): Promise<FlushResult> {
  try {
    return await flushOfflineQueue();
  } catch {
    const remaining = await getQueuedMutations();
    return { uploaded: 0, remaining: remaining.length };
  }
}
