export const CLASSROOM_HUB_PATH = "/hubs/classroom";

export const CLASSROOM_HUB_EVENTS = {
  ParticipantJoined: "ParticipantJoined",
  ParticipantLeft: "ParticipantLeft",
  PresenceSnapshot: "PresenceSnapshot",
  ScreenShareStarted: "ScreenShareStarted",
  ScreenShareStopped: "ScreenShareStopped",
  ContentPresented: "ContentPresented",
  StudentMediaChanged: "StudentMediaChanged",
} as const;

export type ClassroomHubEvent = (typeof CLASSROOM_HUB_EVENTS)[keyof typeof CLASSROOM_HUB_EVENTS];

export type JoinToken = {
  sessionId: string;
  roomId: string;
  token: string;
};

export type StudentMediaState = {
  studentUserId: string;
  audioEnabled: boolean;
  videoEnabled: boolean;
};

export type ClassroomLiveState = {
  participantIds: string[];
  screenShareUserId: string | null;
  presentedContentItemId: string | null;
  studentMedia: Record<string, StudentMediaState>;
};

export function emptyClassroomLiveState(): ClassroomLiveState {
  return {
    participantIds: [],
    screenShareUserId: null,
    presentedContentItemId: null,
    studentMedia: {},
  };
}
