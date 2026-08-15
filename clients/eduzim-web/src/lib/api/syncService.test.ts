import { describe, expect, it } from "vitest";
import { groupByStudentId, toUploadRequest } from "@/lib/api/syncService";
import { OfflineSyncKinds, type QueuedMutation } from "@/lib/offline/types";

function mutation(
  id: string,
  studentId: string,
  kind: QueuedMutation["payload"]["kind"] = OfflineSyncKinds.ModuleProgress,
): QueuedMutation {
  return {
    id,
    clientId: id,
    studentId,
    localTimestamp: "2026-08-14T10:00:00.000Z",
    queuedAt: "2026-08-14T10:00:00.000Z",
    payload: {
      kind,
      moduleId: "11111111-1111-1111-1111-111111111111",
      isCompleted: true,
    },
  };
}

describe("groupByStudentId", () => {
  it("groups queued mutations by student", () => {
    const studentA = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const studentB = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    const items = [
      mutation("1", studentA),
      mutation("2", studentB),
      mutation("3", studentA),
    ];

    const groups = groupByStudentId(items);

    expect(groups.get(studentA)?.map((item) => item.id)).toEqual(["1", "3"]);
    expect(groups.get(studentB)?.map((item) => item.id)).toEqual(["2"]);
  });
});

describe("toUploadRequest", () => {
  it("maps queue records to the sync upload contract", () => {
    const studentId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    const request = toUploadRequest(studentId, [mutation("1", studentId)]);

    expect(request.studentId).toBe(studentId);
    expect(request.items).toEqual([
      {
        clientId: "1",
        localTimestamp: "2026-08-14T10:00:00.000Z",
        payload: {
          kind: OfflineSyncKinds.ModuleProgress,
          moduleId: "11111111-1111-1111-1111-111111111111",
          isCompleted: true,
        },
      },
    ]);
  });
});
