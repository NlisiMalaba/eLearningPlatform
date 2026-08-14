"use client";

import { useQuery } from "@tanstack/react-query";
import { ContentRenderer } from "@/components/learning/ContentRenderer";
import {
  fetchQuizJson,
  fetchTranscriptText,
  getCaptions,
  getContent,
  getTranscript,
} from "@/lib/content/contentService";
import { parseQuizPayload } from "@/lib/content/parseQuizPayload";
import { selectRendererKind } from "@/lib/content/selectRenderer";
import { t } from "@/lib/i18n/t";

type ContentItemPlayerProps = {
  contentItemId: string;
};

export function ContentItemPlayer({ contentItemId }: ContentItemPlayerProps) {
  const contentQuery = useQuery({
    queryKey: ["content", contentItemId],
    queryFn: () => getContent(contentItemId),
  });

  const kind = contentQuery.data ? selectRendererKind(contentQuery.data.type) : null;

  const captionsQuery = useQuery({
    queryKey: ["captions", contentItemId],
    queryFn: () => getCaptions(contentItemId),
    enabled: kind === "video",
  });

  const transcriptQuery = useQuery({
    queryKey: ["transcript", contentItemId],
    queryFn: () => getTranscript(contentItemId),
    enabled: kind === "audio",
  });

  const transcriptTextQuery = useQuery({
    queryKey: ["transcript-text", transcriptQuery.data?.signedUrl],
    queryFn: () => {
      const url = transcriptQuery.data?.signedUrl;
      if (!url) {
        return Promise.resolve(null);
      }

      return fetchTranscriptText(url);
    },
    enabled: Boolean(transcriptQuery.data?.signedUrl),
  });

  const quizQuery = useQuery({
    queryKey: ["quiz", contentItemId, contentQuery.data?.downloadUrl],
    queryFn: async () => {
      const url = contentQuery.data?.downloadUrl;
      if (!url) {
        return parseQuizPayload(null);
      }

      return parseQuizPayload(await fetchQuizJson(url));
    },
    enabled: kind === "quiz" && Boolean(contentQuery.data?.downloadUrl),
  });

  if (contentQuery.isLoading) {
    return <p>{t("content.loading")}</p>;
  }

  if (contentQuery.isError || !contentQuery.data) {
    return <p role="alert">{t("content.error")}</p>;
  }

  return (
    <ContentRenderer
      content={contentQuery.data}
      captions={captionsQuery.data ?? []}
      transcript={transcriptQuery.data ?? null}
      transcriptText={transcriptTextQuery.data ?? null}
      quiz={quizQuery.data ?? null}
    />
  );
}
