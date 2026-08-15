"use client";

import { resolveVideoEmbed } from "@/lib/classroom/videoEmbed";
import { t } from "@/lib/i18n/t";

type VideoRoomEmbedProps = {
  roomId: string;
  token: string;
};

export function VideoRoomEmbed({ roomId, token }: VideoRoomEmbedProps) {
  const embed = resolveVideoEmbed(roomId, token);

  return (
    <iframe
      title={t("classroom.video.title")}
      src={embed.src}
      allow="camera; microphone; autoplay; clipboard-write; display-capture; fullscreen"
      allowFullScreen
      className="h-[min(70vh,32rem)] w-full rounded-xl border border-zinc-200 bg-black"
    />
  );
}
