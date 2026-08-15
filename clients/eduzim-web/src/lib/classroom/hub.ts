import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from "@microsoft/signalr";
import { CLASSROOM_HUB_EVENTS, CLASSROOM_HUB_PATH } from "@/lib/classroom/types";
import { applyClassroomHubEvent, emptyClassroomLiveState } from "@/lib/classroom/mapClassroom";
import type { ClassroomLiveState } from "@/lib/classroom/types";

export async function fetchHubAccessToken(): Promise<string> {
  const response = await fetch("/api/auth/hub-token", {
    method: "GET",
    credentials: "include",
    cache: "no-store",
  });
  if (!response.ok) {
    throw new Error("Classroom hub token was not available.");
  }

  const body = (await response.json()) as { accessToken?: unknown };
  if (typeof body.accessToken !== "string" || body.accessToken.length === 0) {
    throw new Error("Classroom hub token was not available.");
  }

  return body.accessToken;
}

export function createClassroomHubConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(CLASSROOM_HUB_PATH, { accessTokenFactory: fetchHubAccessToken })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 15000])
    .configureLogging(LogLevel.Warning)
    .build();
}

export async function joinClassroomHub(connection: HubConnection, sessionId: string): Promise<void> {
  if (connection.state !== HubConnectionState.Connected) {
    await connection.start();
  }

  await connection.invoke("JoinSession", sessionId);
}

export async function leaveClassroomHub(connection: HubConnection, sessionId: string): Promise<void> {
  if (connection.state === HubConnectionState.Connected) {
    await connection.invoke("LeaveSession", sessionId);
  }

  await connection.stop();
}

export function bindClassroomHubEvents(
  connection: HubConnection,
  onState: (next: ClassroomLiveState) => void,
): () => void {
  let state = emptyClassroomLiveState();
  const names = Object.values(CLASSROOM_HUB_EVENTS);
  for (const name of names) {
    connection.on(name, (payload: unknown) => {
      state = applyClassroomHubEvent(state, name, payload);
      onState(state);
    });
  }

  return () => {
    for (const name of names) {
      connection.off(name);
    }
  };
}
