import { Modal, StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import { TTS_LANGUAGES, type TtsLanguage } from "@/lib/accessibility/speech";
import {
  setAccessibilitySettingsOpen,
  setHighContrast,
  setTtsLanguage,
  useAccessibilityStore,
} from "@/lib/accessibility/store";
import { useReadAloud } from "@/lib/accessibility/useReadableSection";
import { t } from "@/lib/i18n/t";
import { useTheme } from "@/theme";

export function AccessibilitySettings() {
  const { settingsOpen, highContrast, ttsLanguage, speaking } = useAccessibilityStore();
  const { colors } = useTheme();
  const { readAloud, stop } = useReadAloud();
  const styles = makeStyles(colors);

  return (
    <>
      <Focusable
        accessibilityRole="button"
        accessibilityState={{ expanded: settingsOpen }}
        onPress={() => setAccessibilitySettingsOpen(true)}
        style={styles.menu}
      >
        <Text style={styles.menuLabel}>{t("a11y.menu")}</Text>
      </Focusable>
      <Modal
        visible={settingsOpen}
        animationType="slide"
        onRequestClose={() => setAccessibilitySettingsOpen(false)}
        accessibilityViewIsModal
      >
        <View style={styles.panel}>
          <Text style={styles.title}>{t("a11y.panel.title")}</Text>

          <View style={styles.row}>
            <Text style={styles.label}>{t("a11y.contrast.label")}</Text>
            <Focusable
              accessibilityRole="switch"
              accessibilityState={{ checked: highContrast }}
              accessibilityLabel={t("a11y.contrast.label")}
              onPress={() => setHighContrast(!highContrast)}
              style={styles.toggle}
            >
              <Text style={styles.toggleLabel}>
                {highContrast ? t("a11y.contrast.on") : t("a11y.contrast.off")}
              </Text>
            </Focusable>
          </View>

          <Text style={styles.label}>{t("a11y.tts.language")}</Text>
          <View style={styles.chips}>
            {TTS_LANGUAGES.map((language) => {
              const selected = language === ttsLanguage;
              return (
                <Focusable
                  key={language}
                  accessibilityRole="button"
                  accessibilityState={{ selected }}
                  accessibilityLabel={ttsLanguageLabel(language)}
                  onPress={() => setTtsLanguage(language)}
                  style={[styles.chip, selected ? styles.chipOn : null]}
                >
                  <Text style={[styles.chipLabel, selected ? styles.chipLabelOn : null]}>
                    {ttsLanguageLabel(language)}
                  </Text>
                </Focusable>
              );
            })}
          </View>

          <View style={styles.actions}>
            <Focusable
              accessibilityRole="button"
              accessibilityLabel={t("a11y.tts.read")}
              onPress={readAloud}
              style={styles.primary}
            >
              <Text style={styles.primaryLabel}>{t("a11y.tts.read")}</Text>
            </Focusable>
            <Focusable
              accessibilityRole="button"
              accessibilityLabel={t("a11y.tts.stop")}
              disabled={!speaking}
              onPress={stop}
              style={styles.secondary}
            >
              <Text style={styles.secondaryLabel}>{t("a11y.tts.stop")}</Text>
            </Focusable>
          </View>

          <Focusable
            accessibilityRole="button"
            onPress={() => setAccessibilitySettingsOpen(false)}
            style={styles.secondary}
          >
            <Text style={styles.secondaryLabel}>{t("a11y.close")}</Text>
          </Focusable>
        </View>
      </Modal>
    </>
  );
}

function ttsLanguageLabel(language: TtsLanguage): string {
  switch (language) {
    case "en":
      return t("a11y.tts.en");
    case "sn":
      return t("a11y.tts.sn");
    case "nd":
      return t("a11y.tts.nd");
  }
}

function makeStyles(colors: ReturnType<typeof useTheme>["colors"]) {
  return StyleSheet.create({
    menu: {
      alignSelf: "flex-start",
      paddingHorizontal: 4,
    },
    menuLabel: {
      color: colors.brand,
      fontSize: 14,
      fontWeight: "600",
    },
    panel: {
      flex: 1,
      backgroundColor: colors.background,
      padding: 24,
      gap: 16,
    },
    title: {
      fontSize: 22,
      fontWeight: "600",
      color: colors.text,
    },
    row: {
      flexDirection: "row",
      justifyContent: "space-between",
      alignItems: "center",
      gap: 12,
    },
    label: {
      fontSize: 16,
      fontWeight: "600",
      color: colors.text,
      flex: 1,
    },
    toggle: {
      borderWidth: 1,
      borderColor: colors.border,
      borderRadius: 8,
      paddingHorizontal: 12,
    },
    toggleLabel: {
      color: colors.text,
      fontWeight: "600",
    },
    chips: {
      flexDirection: "row",
      flexWrap: "wrap",
      gap: 8,
    },
    chip: {
      borderWidth: 1,
      borderColor: colors.border,
      borderRadius: 999,
      paddingHorizontal: 12,
    },
    chipOn: {
      backgroundColor: colors.brand,
      borderColor: colors.brand,
    },
    chipLabel: {
      color: colors.text,
      fontSize: 14,
    },
    chipLabelOn: {
      color: colors.background,
      fontWeight: "600",
    },
    actions: {
      flexDirection: "row",
      flexWrap: "wrap",
      gap: 8,
    },
    primary: {
      backgroundColor: colors.brand,
      borderRadius: 8,
      paddingHorizontal: 16,
    },
    primaryLabel: {
      color: colors.background,
      fontWeight: "600",
    },
    secondary: {
      borderWidth: 1,
      borderColor: colors.border,
      borderRadius: 8,
      paddingHorizontal: 16,
    },
    secondaryLabel: {
      color: colors.text,
      fontWeight: "600",
    },
  });
}
