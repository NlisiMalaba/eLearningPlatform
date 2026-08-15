import { Modal, StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import type { PreschoolLanguage } from "@/lib/preschool/languages";
import { preschoolT } from "@/lib/preschool/messages";
import { useTheme } from "@/theme";

type SessionPromptProps = {
  kind: "rest" | "pause";
  language: PreschoolLanguage;
  onContinue: () => void;
};

export function SessionPrompt({ kind, language, onContinue }: SessionPromptProps) {
  const { colors } = useTheme();
  const title = kind === "rest" ? preschoolT("rest.title", language) : preschoolT("pause.title", language);
  const body = kind === "rest" ? preschoolT("rest.body", language) : preschoolT("pause.body", language);
  const action = kind === "rest" ? preschoolT("rest.continue", language) : preschoolT("pause.resume", language);

  return (
    <Modal visible animationType="fade" accessibilityViewIsModal>
      <View style={[styles.screen, { backgroundColor: colors.background }]}>
        <Text style={[styles.title, { color: colors.brand }]}>{title}</Text>
        <Text style={[styles.body, { color: colors.text }]}>{body}</Text>
        <Focusable
          accessibilityRole="button"
          accessibilityLabel={action}
          onPress={onContinue}
          style={[styles.button, { backgroundColor: colors.brand }]}
        >
          <Text style={[styles.buttonLabel, { color: colors.background }]}>{action}</Text>
        </Focusable>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    justifyContent: "center",
    padding: 28,
    gap: 16,
  },
  title: {
    fontSize: 32,
    fontWeight: "800",
  },
  body: {
    fontSize: 20,
    lineHeight: 28,
  },
  button: {
    marginTop: 12,
    borderRadius: 16,
    paddingHorizontal: 20,
  },
  buttonLabel: {
    fontSize: 22,
    fontWeight: "700",
  },
});
