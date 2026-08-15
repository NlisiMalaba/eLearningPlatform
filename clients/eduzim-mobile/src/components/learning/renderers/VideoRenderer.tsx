import { Video, ResizeMode } from "expo-av";
import { useEffect, useMemo, useState } from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { inferCaptionFormat } from "@/lib/content/captionFormat";
import { cueTextAt, parseCaptionDocument, type CaptionCue } from "@/lib/content/parseCaptions";
import type { CaptionTrack, ContentType } from "@/lib/content/types";
import { t } from "@/lib/i18n/t";
import { colors } from "@/theme";

type VideoRendererProps = {
  src: string;
  title: string;
  contentType: Extract<ContentType, "Video" | "Animation">;
  captions: CaptionTrack[];
};

export function VideoRenderer({ src, title, contentType, captions }: VideoRendererProps) {
  const labelKey = contentType === "Animation" ? "content.animation.label" : "content.video.label";
  const [selectedTrackId, setSelectedTrackId] = useState(captions[0]?.trackId);
  const [cues, setCues] = useState<CaptionCue[]>([]);
  const [positionMs, setPositionMs] = useState(0);
  const selected = captions.find((track) => track.trackId === selectedTrackId) ?? captions[0];

  useEffect(() => {
    if (!selected) {
      setCues([]);
      return;
    }

    let cancelled = false;
    void fetch(selected.signedUrl)
      .then((response) => (response.ok ? response.text() : ""))
      .then((text) => {
        if (!cancelled) {
          setCues(parseCaptionDocument(text, inferCaptionFormat(selected.signedUrl)));
        }
      })
      .catch(() => {
        if (!cancelled) {
          setCues([]);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [selected?.signedUrl, selected?.trackId]);

  const overlay = useMemo(() => cueTextAt(cues, positionMs), [cues, positionMs]);

  return (
    <View style={styles.stack}>
      <View style={styles.player}>
        <Video
          source={{ uri: src }}
          style={styles.video}
          useNativeControls
          resizeMode={ResizeMode.CONTAIN}
          accessibilityLabel={`${t(labelKey)}: ${title}`}
          onPlaybackStatusUpdate={(status) => {
            if (status.isLoaded) {
              setPositionMs(status.positionMillis);
            }
          }}
        />
        {overlay ? (
          <View pointerEvents="none" style={styles.captionOverlay}>
            <Text style={styles.captionText}>{overlay}</Text>
          </View>
        ) : null}
      </View>
      {captions.length === 0 ? (
        <Text style={styles.muted}>{t("content.video.captionsMissing")}</Text>
      ) : (
        <View style={styles.tracks}>
          {captions.map((track) => {
            const on = track.trackId === selected?.trackId;
            return (
              <Pressable
                key={track.trackId}
                accessibilityRole="button"
                accessibilityState={{ selected: on }}
                onPress={() => setSelectedTrackId(track.trackId)}
                style={[styles.track, on ? styles.trackOn : null]}
              >
                <Text style={[styles.trackLabel, on ? styles.trackLabelOn : null]}>{track.language}</Text>
              </Pressable>
            );
          })}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  stack: {
    gap: 12,
  },
  player: {
    width: "100%",
    aspectRatio: 16 / 9,
    backgroundColor: "#000000",
    borderRadius: 12,
    overflow: "hidden",
  },
  video: {
    width: "100%",
    height: "100%",
  },
  captionOverlay: {
    position: "absolute",
    left: 12,
    right: 12,
    bottom: 48,
    alignItems: "center",
  },
  captionText: {
    backgroundColor: "rgba(0,0,0,0.72)",
    color: "#FFFFFF",
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 6,
    textAlign: "center",
    fontSize: 14,
  },
  muted: {
    color: colors.muted,
    fontSize: 14,
  },
  tracks: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
  },
  track: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 999,
    paddingHorizontal: 12,
    paddingVertical: 8,
    minHeight: 44,
    justifyContent: "center",
  },
  trackOn: {
    backgroundColor: colors.brand,
    borderColor: colors.brand,
  },
  trackLabel: {
    fontSize: 13,
    color: colors.text,
  },
  trackLabelOn: {
    color: colors.surface,
    fontWeight: "600",
  },
});
