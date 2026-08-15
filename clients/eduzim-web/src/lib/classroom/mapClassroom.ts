import { CLASSROOM_HUB_EVENTS, type ClassroomLiveState, type JoinToken } from "@/lib/classroom/types";
import { readBoolean, readField, readGuid, readString } from "@/lib/classroom/parse";

export function mapJoinToken(raw: Record<string, unknown>): JoinToken | null {
  const sessionId = readGuid(readField(raw, "sessionId", "SessionId"));
  const roomId = readString(readField(raw, "roomId", "RoomId"));
  const token = readString(readField(raw, "token", "Token"));
  if (!sessionId || !roomId || !token) {
    return null;
  }

  return { sessionId, roomId, token };
}

export function applyClassroomHubEvent(
  state: ClassroomLiveState,
  eventName: string,
  payload: unknown,
): ClassroomLiveState {
  const raw = asRecord(payload);
  switch (eventName) {
    case CLASSROOM_HUB_EVENTS.PresenceSnapshot:
      return mapSnapshot(raw);
    case CLASSROOM_HUB_EVENTS.ParticipantJoined: {
      const userId = readUserId(raw);
      if (!userId || state.participantIds.includes(userId)) {
        return state;
      }

      return { ...state, participantIds: [...state.participantIds, userId] };
    }
    case CLASSROOM_HUB_EVENTS.ParticipantLeft: {
      const userId = readUserId(raw);
      if (!userId) {
        return state;
      }

      return {
        ...state,
        participantIds: state.participantIds.filter((id) => id !== userId),
      };
    }
    case CLASSROOM_HUB_EVENTS.ScreenShareStarted:
      return { ...state, screenShareUserId: readUserId(raw) };
    case CLASSROOM_HUB_EVENTS.ScreenShareStopped:
      return { ...state, screenShareUserId: null };
    case CLASSROOM_HUB_EVENTS.ContentPresented:
      return {
        ...state,
        presentedContentItemId: readGuid(readField(raw, "contentItemId", "ContentItemId")) ?? null,
      };
    case CLASSROOM_HUB_EVENTS.StudentMediaChanged: {
      const studentUserId = readGuid(readField(raw, "studentUserId", "StudentUserId"));
      if (!studentUserId) {
        return state;
      }

      const media = {
        studentUserId,
        audioEnabled: readBoolean(readField(raw, "audioEnabled", "AudioEnabled")),
        videoEnabled: readBoolean(readField(raw, "videoEnabled", "VideoEnabled")),
      };
      return {
        ...state,
        studentMedia: { ...state.studentMedia, [studentUserId]: media },
      };
    }
    default:
      return state;
  }
}

function mapSnapshot(raw: Record<string, unknown>): ClassroomLiveState {
  const idsRaw = readField(raw, "participantUserIds", "ParticipantUserIds");
  const ids = Array.isArray(idsRaw)
    ? idsRaw.flatMap((id) => {
        const guid = readGuid(id);
        return guid ? [guid] : [];
      })
    : [];
  const mediaRows = readField(raw, "studentMedia", "StudentMedia");
  const studentMedia: ClassroomLiveState["studentMedia"] = {};
  if (Array.isArray(mediaRows)) {
    for (const row of mediaRows) {
      const record = asRecord(row);
      const studentUserId = readGuid(readField(record, "studentUserId", "StudentUserId"));
      if (!studentUserId) {
        continue;
      }

      studentMedia[studentUserId] = {
        studentUserId,
        audioEnabled: readBoolean(readField(record, "audioEnabled", "AudioEnabled")),
        videoEnabled: readBoolean(readField(record, "videoEnabled", "VideoEnabled")),
      };
    }
  }

  return {
    participantIds: ids,
    screenShareUserId: readGuid(readField(raw, "screenShareUserId", "ScreenShareUserId")) ?? null,
    presentedContentItemId: readGuid(readField(raw, "presentedContentItemId", "PresentedContentItemId")) ?? null,
    studentMedia,
  };
}

function readUserId(raw: Record<string, unknown>): string | null {
  return readGuid(readField(raw, "userId", "UserId")) ?? null;
}

function asRecord(value: unknown): Record<string, unknown> {
  if (value && typeof value === "object") {
    return value as Record<string, unknown>;
  }

  return {};
}

export { emptyClassroomLiveState } from "@/lib/classroom/types";
