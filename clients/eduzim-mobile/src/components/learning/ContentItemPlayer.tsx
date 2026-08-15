import { useEffect, useState } from "react";
import { ActivityIndicator, Text, View } from "react-native";
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
import type { CaptionTrack, ContentDetail, QuizPayload, TranscriptLink } from "@/lib/content/types";
import { setLearningContext } from "@/lib/learning/context";
import { t } from "@/lib/i18n/t";
import { useTheme } from "@/theme";

type ContentItemPlayerProps = {
  contentItemId: string;
  moduleId: string;
};

type PlayerState =
  | { status: "loading" }
  | { status: "error" }
  | {
      status: "ready";
      content: ContentDetail;
      captions: CaptionTrack[];
      transcript: TranscriptLink | null;
      transcriptText: string | null;
      quiz: QuizPayload | null;
    };

export function ContentItemPlayer({ contentItemId, moduleId }: ContentItemPlayerProps) {
  const { colors } = useTheme();
  const [state, setState] = useState<PlayerState>({ status: "loading" });

  useEffect(() => {
    let cancelled = false;
    setState({ status: "loading" });

    void (async () => {
      try {
        const content = await getContent(contentItemId);
        const kind = selectRendererKind(content.type);
        setLearningContext({ moduleId, inAssessment: kind === "quiz" });

        const [captions, transcript, quiz] = await Promise.all([
          kind === "video" ? getCaptions(contentItemId) : Promise.resolve([]),
          kind === "audio" ? getTranscript(contentItemId) : Promise.resolve(null),
          kind === "quiz" && content.downloadUrl
            ? parseQuizPayload(await fetchQuizJson(content.downloadUrl))
            : Promise.resolve(null),
        ]);

        const transcriptText = transcript?.signedUrl
          ? await fetchTranscriptText(transcript.signedUrl)
          : null;

        if (!cancelled) {
          setState({
            status: "ready",
            content,
            captions,
            transcript,
            transcriptText,
            quiz,
          });
        }
      } catch {
        if (!cancelled) {
          setState({ status: "error" });
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [contentItemId, moduleId]);

  if (state.status === "loading") {
    return (
      <View style={{ paddingVertical: 24 }}>
        <ActivityIndicator color={colors.brand} />
        <Text style={{ marginTop: 8, color: colors.muted }}>{t("content.loading")}</Text>
      </View>
    );
  }

  if (state.status === "error") {
    return (
      <Text accessibilityRole="alert" style={{ color: colors.danger }}>
        {t("content.error")}
      </Text>
    );
  }

  return (
    <ContentRenderer
      content={state.content}
      captions={state.captions}
      transcript={state.transcript}
      transcriptText={state.transcriptText}
      quiz={state.quiz}
    />
  );
}
