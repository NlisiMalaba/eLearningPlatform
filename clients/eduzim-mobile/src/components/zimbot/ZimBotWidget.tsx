import { StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import { ZimBotPanel } from "@/components/zimbot/ZimBotPanel";
import { getStudentId } from "@/lib/auth/session";
import { t } from "@/lib/i18n/t";
import { setZimBotOpen, useZimBotStore } from "@/lib/zimbot/store";
import { useTheme } from "@/theme";

export function ZimBotWidget() {
  const { colors } = useTheme();
  const { open } = useZimBotStore();
  const studentId = getStudentId();

  if (!studentId) {
    return null;
  }

  return (
    <View pointerEvents="box-none" style={styles.anchor}>
      {open ? (
        <View style={styles.panelWrap}>
          <ZimBotPanel studentId={studentId} onClose={() => setZimBotOpen(false)} />
        </View>
      ) : null}
      <Focusable
        accessibilityRole="button"
        accessibilityLabel={t("zimbot.launcher")}
        accessibilityState={{ expanded: open }}
        onPress={() => setZimBotOpen(!open)}
        style={[styles.launcher, { backgroundColor: colors.brand }]}
      >
        <Text style={[styles.launcherLabel, { color: colors.background }]}>{t("zimbot.launcher")}</Text>
      </Focusable>
    </View>
  );
}

const styles = StyleSheet.create({
  anchor: {
    position: "absolute",
    right: 16,
    bottom: 16,
    left: 16,
    alignItems: "flex-end",
    gap: 12,
  },
  panelWrap: {
    width: "100%",
    maxWidth: 420,
  },
  launcher: {
    borderRadius: 999,
    paddingHorizontal: 16,
    shadowColor: "#000",
    shadowOpacity: 0.2,
    shadowRadius: 8,
    elevation: 4,
  },
  launcherLabel: {
    fontWeight: "600",
    fontSize: 14,
  },
});
