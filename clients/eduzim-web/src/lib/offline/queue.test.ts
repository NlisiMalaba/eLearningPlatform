import { afterEach, describe, expect, it, vi } from "vitest";
import { enqueueMutation, getQueuedMutations, clearQueuedMutations } from "@/lib/offline/queue";
import { flushOfflineQueue, tryFlushOfflineQueue } from "@/lib/offline/flushQueue";
import { OfflineSyncKinds } from "@/lib/offline/types";

vi.mock("@/lib/api/syncService", async () => {
  const actual = await vi.importActual<typeof import("@/lib/api/syncService")>(
    "@/lib/api/syncService",
  );
  return {
    ...actual,
    uploadOfflineQueue: vi.fn(),
  };
});

import { uploadOfflineQueue } from "@/lib/api/syncService";

const mockedUpload = vi.mocked(uploadOfflineQueue);

afterEach(async () => {
  await clearQueuedMutations();
  vi.clearAllMocks();
});

describe("offline queue", () => {
  it("persists failed mutations in IndexedDB", async () => {
    const studentId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    await enqueueMutation(studentId, {
      kind: OfflineSyncKinds.ModuleProgress,
      moduleId: "11111111-1111-1111-1111-111111111111",
      isCompleted: true,
    });

    const queued = await getQueuedMutations();
    expect(queued).toHaveLength(1);
    expect(queued[0]?.studentId).toBe(studentId);
    expect(queued[0]?.payload.kind).toBe(OfflineSyncKinds.ModuleProgress);
  });

  it("uploads queued mutations and removes them after success", async () => {
    mockedUpload.mockResolvedValue({
      acceptedCount: 1,
      syncedCount: 1,
      conflictedCount: 0,
    });

    const studentId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    await enqueueMutation(studentId, {
      kind: OfflineSyncKinds.AssessmentAttempt,
      assessmentId: "22222222-2222-2222-2222-222222222222",
      scorePercent: 80,
    });

    const result = await flushOfflineQueue();

    expect(result.uploaded).toBe(1);
    expect(result.remaining).toBe(0);
    expect(mockedUpload).toHaveBeenCalledOnce();
    expect(await getQueuedMutations()).toHaveLength(0);
  });

  it("keeps queued mutations when upload fails", async () => {
    mockedUpload.mockRejectedValue(new Error("offline"));

    const studentId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    await enqueueMutation(studentId, {
      kind: OfflineSyncKinds.ModuleProgress,
      moduleId: "11111111-1111-1111-1111-111111111111",
    });

    const result = await tryFlushOfflineQueue();

    expect(result.uploaded).toBe(0);
    expect(result.remaining).toBe(1);
    expect(await getQueuedMutations()).toHaveLength(1);
  });
});
