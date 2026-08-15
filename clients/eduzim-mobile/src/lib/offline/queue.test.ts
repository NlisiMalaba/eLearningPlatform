import { afterEach, describe, expect, it, vi } from "vitest";
import { configureOfflineQueue, enqueueMutation, getQueuedMutations, clearQueuedMutations } from "@/lib/offline/queue";
import { createMemoryQueueStore } from "@/lib/offline/memoryQueueStore";
import { flushOfflineQueue, tryFlushOfflineQueue } from "@/lib/offline/flushQueue";
import { OfflineSyncKinds } from "@/lib/offline/types";
import { isNetInfoOnline, subscribeConnectivityRestore } from "@/lib/offline/syncOnReconnect";
import { groupByStudentId, toUploadRequest } from "@/lib/api/syncService";

vi.mock("@/lib/api/syncService", async () => {
  const actual = await vi.importActual<typeof import("@/lib/api/syncService")>("@/lib/api/syncService");
  return {
    ...actual,
    uploadOfflineQueue: vi.fn(),
  };
});

import { uploadOfflineQueue } from "@/lib/api/syncService";

const mockedUpload = vi.mocked(uploadOfflineQueue);

configureOfflineQueue(createMemoryQueueStore());

afterEach(async () => {
  configureOfflineQueue(createMemoryQueueStore());
  await clearQueuedMutations();
  vi.clearAllMocks();
});

describe("offline sync queue", () => {
  it("writes OfflineSyncQueue records locally", async () => {
    configureOfflineQueue(createMemoryQueueStore());
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

  it("uploads queued progress to POST /sync/upload and removes synced rows", async () => {
    configureOfflineQueue(createMemoryQueueStore());
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

  it("keeps queued records when upload fails", async () => {
    configureOfflineQueue(createMemoryQueueStore());
    mockedUpload.mockRejectedValue(new Error("offline"));
    await enqueueMutation("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", {
      kind: OfflineSyncKinds.ModuleProgress,
      moduleId: "11111111-1111-1111-1111-111111111111",
    });

    const result = await tryFlushOfflineQueue();
    expect(result.uploaded).toBe(0);
    expect(result.remaining).toBe(1);
  });

  it("maps queue rows to the API upload contract grouped by student", () => {
    const studentA = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const item = {
      id: "1",
      clientId: "1",
      studentId: studentA,
      localTimestamp: "2026-08-15T07:00:00.000Z",
      queuedAt: "2026-08-15T07:00:00.000Z",
      payload: {
        kind: OfflineSyncKinds.ModuleProgress,
        moduleId: "11111111-1111-1111-1111-111111111111",
        isCompleted: true,
      },
    };
    const request = toUploadRequest(studentA, [item]);
    expect(request.studentId).toBe(studentA);
    expect(request.items[0]?.clientId).toBe("1");
    expect(groupByStudentId([item, { ...item, id: "2", clientId: "2", studentId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" }]).size).toBe(2);
  });
});

describe("connectivity restore", () => {
  it("treats reachable NetInfo as online", () => {
    expect(isNetInfoOnline({ isConnected: true, isInternetReachable: true })).toBe(true);
    expect(isNetInfoOnline({ isConnected: false, isInternetReachable: false })).toBe(false);
    expect(isNetInfoOnline({ isConnected: true, isInternetReachable: false })).toBe(false);
  });

  it("flushes when connectivity returns", async () => {
    const listeners: Array<(online: boolean) => void> = [];
    const onOnline = vi.fn();
    const unsubscribe = subscribeConnectivityRestore(
      {
        getIsOnline: () => false,
        subscribe: (listener) => {
          listeners.push(listener);
          return () => undefined;
        },
      },
      onOnline,
    );

    await Promise.resolve();
    expect(onOnline).not.toHaveBeenCalled();
    listeners[0]?.(true);
    await Promise.resolve();
    expect(onOnline).toHaveBeenCalledOnce();
    unsubscribe();
  });
});
