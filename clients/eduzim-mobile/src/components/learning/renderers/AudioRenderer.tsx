import { Audio, type AVPlaybackStatusSuccess } from "expo-av";
import { useEffect, useState } from "react";
import { Linking, Pressable, StyleSheet, Text, View } from "react-native";
import { t } from "@/lib/i18n/t";
import { colors } from "@/theme";

type AudioRendererProps = {
  src: string;
  title: string;
  transcriptUrl: string | null;
  transcriptText: string | null;
};

export function AudioRenderer({
  src,
  title,
  transcriptUrl,
  transcriptText,
}: AudioRendererProps) {
  const [sound, setSound] = useState<Audio.Sound | null>(null);
  const [playing, setPlaying] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    let loaded: Audio.Sound | null = null;

    void (async () => {
      try {
        await Audio.setAudioModeAsync({ playsInSilentModeIOS: true, staysActiveInBackground: false });
        const created = await Audio.Sound.createAsync(
          { uri: src },
          { shouldPlay: false },
          (status) => {
            if (!status.isLoaded) {
              return;
            }

            const loadedStatus = status as AVPlaybackStatusSuccess;
            setPlaying(loadedStatus.isPlaying);
          },
        );
        if (cancelled) {
          await created.sound.unloadAsync();
          return;
        }

        loaded = created.sound;
        setSound(created.sound);
      } catch {
        if (!cancelled) {
          setError(t("content.error"));
        }
      }
    })();

    return () => {
      cancelled = true;
      void loaded?.unloadAsync();
    };
  }, [src]);

  async function togglePlayback(): Promise<void> {
    if (!sound) {
      return;
    }

    const status = await sound.getStatusAsync();
    if (!status.isLoaded) {
      return;
    }

    if (status.isPlaying) {
      await sound.pauseAsync();
      return;
    }

    await sound.playAsync();
  }

  return (
    <View style={styles.stack}>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={`${playing ? t("content.audio.pause") : t("content.audio.play")}: ${title}`}
        onPress={() => {
          void togglePlayback();
        }}
        style={styles.play}
      >
        <Text style={styles.playLabel}>
          {playing ? t("content.audio.pause") : t("content.audio.play")}
        </Text>
      </Pressable>
      {error ? (
        <Text accessibilityRole="alert" style={styles.error}>
          {error}
        </Text>
      ) : null}
      {transcriptUrl ? (
        <View accessibilityLabel={t("content.audio.transcript")} style={styles.transcript}>
          <View style={styles.transcriptHeader}>
            <Text style={styles.transcriptTitle}>{t("content.audio.transcript")}</Text>
            <Pressable
              accessibilityRole="link"
              onPress={() => {
                void Linking.openURL(transcriptUrl);
              }}
            >
              <Text style={styles.link}>{t("content.audio.transcriptOpen")}</Text>
            </Pressable>
          </View>
          {transcriptText ? <Text style={styles.transcriptBody}>{transcriptText}</Text> : null}
        </View>
      ) : (
        <Text style={styles.muted}>{t("content.audio.transcriptMissing")}</Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  stack: {
    gap: 16,
  },
  play: {
    alignSelf: "flex-start",
    backgroundColor: colors.brand,
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 10,
  },
  playLabel: {
    color: colors.surface,
    fontSize: 14,
    fontWeight: "600",
  },
  transcript: {
    borderColor: colors.border,
    borderRadius: 12,
    borderWidth: 1,
    backgroundColor: colors.surface,
    padding: 16,
    gap: 12,
  },
  transcriptHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    gap: 12,
  },
  transcriptTitle: {
    fontSize: 14,
    fontWeight: "600",
    color: colors.text,
  },
  transcriptBody: {
    fontSize: 14,
    color: colors.text,
    lineHeight: 20,
  },
  link: {
    color: colors.brand,
    fontSize: 14,
    fontWeight: "600",
    textDecorationLine: "underline",
  },
  muted: {
    color: colors.muted,
    fontSize: 14,
  },
  error: {
    color: colors.danger,
    fontSize: 14,
  },
});
