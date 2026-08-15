import { describe, expect, it } from "vitest";
import { applyClassroomHubEvent, mapJoinToken } from "@/lib/classroom/mapClassroom";
import { isClassroomSessionId } from "@/lib/classroom/parse";
import { emptyClassroomLiveState } from "@/lib/classroom/types";
import { resolveVideoEmbed } from "@/lib/classroom/videoEmbed";

const sessionId = "11111111-1111-1111-1111-111111111111";
const userId = "22222222-2222-2222-2222-222222222222";

describe("classroom join mapping", () => {
  it("maps a complete join token", () => {
    expect(
      mapJoinToken({
        sessionId,
        roomId: "room-a",
        token: "local-join:room-a:student",
      }),
    ).toEqual({
      sessionId,
      roomId: "room-a",
      token: "local-join:room-a:student",
    });
    expect(mapJoinToken({ sessionId, roomId: "", token: "x" })).toBeNull();
  });

  it("accepts classroom session identifiers", () => {
    expect(isClassroomSessionId(sessionId)).toBe(true);
    expect(isClassroomSessionId("not-a-guid")).toBe(false);
  });
});

describe("video embed", () => {
  it("uses Jitsi for local join tokens", () => {
    const embed = resolveVideoEmbed("room-a", "local-join:room-a:user", "eduzim");
    expect(embed.provider).toBe("jitsi");
    expect(embed.src).toBe("https://meet.jit.si/room-a");
  });

  it("uses Daily.co when a domain and meeting token are provided", () => {
    const embed = resolveVideoEmbed("room-a", "meeting-token", "eduzim");
    expect(embed.provider).toBe("daily");
    expect(embed.src).toBe("https://eduzim.daily.co/room-a?t=meeting-token");
  });
});

describe("classroom hub events", () => {
  it("applies presence, screen share, content, and student media", () => {
    let state = emptyClassroomLiveState();
    state = applyClassroomHubEvent(state, "PresenceSnapshot", {
      participantUserIds: [userId],
      screenShareUserId: null,
      presentedContentItemId: null,
      studentMedia: [],
    });
    state = applyClassroomHubEvent(state, "ParticipantJoined", { userId: sessionId });
    state = applyClassroomHubEvent(state, "ScreenShareStarted", { userId });
    state = applyClassroomHubEvent(state, "ContentPresented", {
      contentItemId: sessionId,
    });
    state = applyClassroomHubEvent(state, "StudentMediaChanged", {
      studentUserId: sessionId,
      audioEnabled: false,
      videoEnabled: true,
    });

    expect(state.participantIds).toEqual([userId, sessionId]);
    expect(state.screenShareUserId).toBe(userId);
    expect(state.presentedContentItemId).toBe(sessionId);
    expect(state.studentMedia[sessionId]?.audioEnabled).toBe(false);
  });
});
