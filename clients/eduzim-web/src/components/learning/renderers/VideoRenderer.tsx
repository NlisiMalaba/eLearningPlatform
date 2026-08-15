"use client";

import type { CaptionTrack, ContentType } from "@/lib/content/types";
import { t } from "@/lib/i18n/t";

type VideoRendererProps = {
  src: string;
  title: string;
  contentType: Extract<ContentType, "Video" | "Animation">;
  captions: CaptionTrack[];
};

export function VideoRenderer({ src, title, contentType, captions }: VideoRendererProps) {
  const labelKey = contentType === "Animation" ? "content.animation.label" : "content.video.label";

  return (
    <div className="flex flex-col gap-3">
      <video
        className="w-full rounded-xl bg-black"
        controls
        crossOrigin="anonymous"
        aria-label={`${t(labelKey)}: ${title}`}
      >
        <source src={src} />
        {captions.map((track, index) => (
          <track
            key={track.trackId}
            kind="captions"
            src={track.signedUrl}
            srcLang={track.language}
            label={track.language}
            default={index === 0}
          />
        ))}
      </video>
      {captions.length === 0 ? (
        <p className="text-sm text-zinc-600">{t("content.video.captionsMissing")}</p>
      ) : null}
    </div>
  );
}
