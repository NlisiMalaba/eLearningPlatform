import { Pressable, StyleSheet, Text, View } from "react-native";
import { ZimBotPanel } from "@/components/zimbot/ZimBotPanel";
import { getStudentId } from "@/lib/auth/session";
import { t } from "@/lib/i18n/t";
import { setZimBotOpen, useZimBotStore } from "@/lib/zimbot/store";
import { colors } from "@/theme";

export function ZimBotWidget() {
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
      <Pressable
        accessibilityRole="button"
        accessibilityState={{ expanded: open }}
        onPress={() => setZimBotOpen(!open)}
        style={styles.launcher}
      >
        <Text style={styles.launcherLabel}>{t("zimbot.launcher")}</Text>
      </Pressable>
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
    backgroundColor: colors.brand,
    borderRadius: 999,
    paddingHorizontal: 16,
    paddingVertical: 12,
    minHeight: 44,
    justifyContent: "center",
    shadowColor: "#000",
    shadowOpacity: 0.2,
    shadowRadius: 8,
    elevation: 4,
  },
  launcherLabel: {
    color: colors.surface,
    fontWeight: "600",
    fontSize: 14,
  },
});
