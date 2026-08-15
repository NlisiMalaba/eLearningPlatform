"use client";

import dynamic from "next/dynamic";
import type { CaptionTrack, ContentDetail, QuizPayload, TranscriptLink } from "@/lib/content/types";
import { selectRendererKind } from "@/lib/content/selectRenderer";
import { AudioRenderer } from "@/components/learning/renderers/AudioRenderer";
import { PdfRenderer } from "@/components/learning/renderers/PdfRenderer";
import { QuizRenderer } from "@/components/learning/renderers/QuizRenderer";
import { UnsupportedRenderer } from "@/components/learning/renderers/UnsupportedRenderer";
import { VideoRenderer } from "@/components/learning/renderers/VideoRenderer";

const Scene3DRenderer = dynamic(
  () =>
    import("@/components/learning/renderers/Scene3DRenderer").then(
      (module) => module.Scene3DRenderer,
    ),
  { ssr: false },
);

type ContentRendererProps = {
  content: ContentDetail;
  captions: CaptionTrack[];
  transcript: TranscriptLink | null;
  transcriptText: string | null;
  quiz: QuizPayload | null;
};

export function ContentRenderer({
  content,
  captions,
  transcript,
  transcriptText,
  quiz,
}: ContentRendererProps) {
  const kind = selectRendererKind(content.type);

  if (kind === "video" && (content.type === "Video" || content.type === "Animation")) {
    return (
      <VideoRenderer
        src={content.downloadUrl}
        title={content.title}
        contentType={content.type}
        captions={captions}
      />
    );
  }

  if (kind === "pdf") {
    return <PdfRenderer src={content.downloadUrl} title={content.title} />;
  }

  if (kind === "audio") {
    return (
      <AudioRenderer
        src={content.downloadUrl}
        title={content.title}
        transcriptUrl={transcript?.signedUrl ?? null}
        transcriptText={transcriptText}
      />
    );
  }

  if (kind === "scene3d") {
    return <Scene3DRenderer src={content.downloadUrl} title={content.title} />;
  }

  if (kind === "quiz") {
    return <QuizRenderer quiz={quiz ?? { questions: [] }} />;
  }

  return <UnsupportedRenderer />;
}
