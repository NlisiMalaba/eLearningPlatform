"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { HubConnectionState, type HubConnection } from "@microsoft/signalr";
import { TeacherClassroomControls } from "@/components/classroom/TeacherClassroomControls";
import { VideoRoomEmbed } from "@/components/classroom/VideoRoomEmbed";
import { ApiError } from "@/lib/api/errors";
import { endClassroom, getJoinToken } from "@/lib/classroom/classroomService";
import {
  bindClassroomHubEvents,
  createClassroomHubConnection,
  joinClassroomHub,
  leaveClassroomHub,
} from "@/lib/classroom/hub";
import { emptyClassroomLiveState } from "@/lib/classroom/types";
import type { ClassroomLiveState, JoinToken } from "@/lib/classroom/types";
import { t } from "@/lib/i18n/t";
import { listContent, type ContentLibraryItem } from "@/lib/teacher/teacherContentService";

type ClassroomRoomProps = {
  sessionId: string;
  currentUserId?: string;
  canControl: boolean;
};

export function ClassroomRoom({ sessionId, currentUserId, canControl }: ClassroomRoomProps) {
  const [join, setJoin] = useState<JoinToken | null>(null);
  const [live, setLive] = useState<ClassroomLiveState>(emptyClassroomLiveState());
  const [contentItems, setContentItems] = useState<ContentLibraryItem[]>([]);
  const [status, setStatus] = useState<"loading" | "ready" | "reconnecting" | "error" | "ended">("loading");
  const [errorDetail, setErrorDetail] = useState<string | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);
  const unbindRef = useRef<(() => void) | null>(null);

  const connect = useCallback(async (): Promise<void> => {
    setStatus("loading");
    setErrorDetail(null);
    try {
      const token = await getJoinToken(sessionId);
      setJoin(token);
      const previous = connectionRef.current;
      unbindRef.current?.();
      unbindRef.current = null;
      if (previous) {
        await leaveClassroomHub(previous, sessionId).catch(() => undefined);
      }

      const connection = createClassroomHubConnection();
      connectionRef.current = connection;
      unbindRef.current = bindClassroomHubEvents(connection, setLive);
      connection.onreconnecting(() => setStatus("reconnecting"));
      connection.onreconnected(async () => {
        await connection.invoke("JoinSession", sessionId);
        setStatus("ready");
      });
      connection.onclose(() => {
        if (connectionRef.current === connection) {
          setStatus("reconnecting");
        }
      });
      await joinClassroomHub(connection, sessionId);
      setStatus("ready");
    } catch (error: unknown) {
      if (error instanceof ApiError && (error.status === 409 || error.status === 404)) {
        setStatus("ended");
        return;
      }

      setErrorDetail(error instanceof Error ? error.message : null);
      setStatus("error");
    }
  }, [sessionId]);

  useEffect(() => {
    void connect();
    return () => {
      unbindRef.current?.();
      unbindRef.current = null;
      const connection = connectionRef.current;
      connectionRef.current = null;
      if (connection) {
        void leaveClassroomHub(connection, sessionId);
      }
    };
  }, [connect, sessionId]);

  useEffect(() => {
    if (!canControl) {
      return;
    }

    let cancelled = false;
    void listContent()
      .then((items) => {
        if (!cancelled) {
          setContentItems(items.filter((item) => item.status === "Published"));
        }
      })
      .catch(() => {
        if (!cancelled) {
          setContentItems([]);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [canControl]);

  const ownMedia = currentUserId ? live.studentMedia[currentUserId] : undefined;
  const audioBlocked = ownMedia?.audioEnabled === false;
  const videoBlocked = ownMedia?.videoEnabled === false;

  async function invokeControl(method: string, ...args: unknown[]): Promise<void> {
    const connection = connectionRef.current;
    if (!connection || connection.state !== HubConnectionState.Connected) {
      throw new Error("Classroom hub is not connected.");
    }

    await connection.invoke(method, sessionId, ...args);
  }

  if (status === "loading" && !join) {
    return <p>{t("classroom.loading")}</p>;
  }

  if (status === "ended") {
    return <p role="alert">{t("classroom.ended")}</p>;
  }

  if (status === "error" || !join) {
    return (
      <div className="flex flex-col gap-3">
        <p role="alert">{t("classroom.error")}</p>
        {errorDetail ? <p className="text-sm text-zinc-600">{errorDetail}</p> : null}
        <button type="button" className="self-start font-medium text-[#0B6E4F] underline" onClick={() => void connect()}>
          {t("classroom.rejoin")}
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t("classroom.title")}</h1>
        <p className="mt-2 text-sm text-zinc-600">
          {t("classroom.presence").replace("{count}", String(live.participantIds.length))}
        </p>
        {status === "reconnecting" ? <p className="mt-2 text-sm text-amber-800">{t("classroom.reconnecting")}</p> : null}
      </header>
      {audioBlocked || videoBlocked ? (
        <p role="status" className="rounded-lg border border-amber-300 bg-amber-50 px-3 py-2 text-sm">
          {audioBlocked && videoBlocked
            ? t("classroom.media.bothOff")
            : audioBlocked
              ? t("classroom.media.audioOff")
              : t("classroom.media.videoOff")}
        </p>
      ) : null}
      {live.presentedContentItemId ? (
        <p className="text-sm">
          {t("classroom.presenting")}: <span className="font-mono">{live.presentedContentItemId}</span>
        </p>
      ) : null}
      {live.screenShareUserId ? (
        <p className="text-sm">{t("classroom.sharing").replace("{user}", live.screenShareUserId.slice(0, 8))}</p>
      ) : null}
      <VideoRoomEmbed roomId={join.roomId} token={join.token} />
      {canControl ? (
        <TeacherClassroomControls
          currentUserId={currentUserId}
          sharing={live.screenShareUserId === currentUserId}
          participantIds={live.participantIds}
          studentMedia={live.studentMedia}
          contentItems={contentItems}
          onStartShare={() => invokeControl("StartScreenShare")}
          onStopShare={() => invokeControl("StopScreenShare")}
          onPresent={(contentItemId) => invokeControl("PresentContent", contentItemId)}
          onSetAudio={(studentUserId, enabled) => invokeControl("SetStudentAudio", studentUserId, enabled)}
          onSetVideo={(studentUserId, enabled) => invokeControl("SetStudentVideo", studentUserId, enabled)}
          onEnd={async () => {
            await endClassroom(sessionId);
            setStatus("ended");
            const connection = connectionRef.current;
            if (connection) {
              await leaveClassroomHub(connection, sessionId).catch(() => undefined);
            }
          }}
        />
      ) : (
        <button type="button" className="self-start font-medium text-[#0B6E4F] underline" onClick={() => void connect()}>
          {t("classroom.rejoin")}
        </button>
      )}
    </div>
  );
}
