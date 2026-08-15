import { apiFetch } from "@/lib/api/client";
import { mapJoinToken } from "@/lib/classroom/mapClassroom";
import type { JoinToken } from "@/lib/classroom/types";

export async function getJoinToken(sessionId: string): Promise<JoinToken> {
  const raw = await apiFetch<Record<string, unknown>>(`/api/v1/classrooms/${sessionId}/join`);
  const mapped = mapJoinToken(raw);
  if (!mapped) {
    throw new Error("Join token response was incomplete.");
  }

  return mapped;
}

export async function scheduleClassroom(input: {
  schoolClassId: string;
  startAtUtc: string;
  durationMinutes: number;
}): Promise<string> {
  const raw = await apiFetch<Record<string, unknown>>("/api/v1/classrooms", {
    method: "POST",
    body: JSON.stringify({
      schoolClassId: input.schoolClassId,
      startAtUtc: input.startAtUtc,
      durationMinutes: input.durationMinutes,
    }),
  });
  const sessionId = typeof raw.sessionId === "string" ? raw.sessionId : "";
  if (!sessionId) {
    throw new Error("Classroom session was not created.");
  }

  return sessionId;
}

export async function endClassroom(sessionId: string): Promise<void> {
  await apiFetch(`/api/v1/classrooms/${sessionId}/end`, { method: "POST" });
}
