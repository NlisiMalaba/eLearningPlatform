import { useEffect, useRef } from "react";
import { Animated, Modal, StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import type { PreschoolLanguage } from "@/lib/preschool/languages";
import { preschoolT } from "@/lib/preschool/messages";
import { useTheme } from "@/theme";

type CelebrationOverlayProps = {
  visible: boolean;
  language: PreschoolLanguage;
  onDone: () => void;
};

export function CelebrationOverlay({ visible, language, onDone }: CelebrationOverlayProps) {
  const { colors } = useTheme();
  const scale = useRef(new Animated.Value(0.4)).current;

  useEffect(() => {
    if (!visible) {
      return;
    }

    const pulse = Animated.sequence([
      Animated.spring(scale, { toValue: 1.15, useNativeDriver: true, friction: 4 }),
      Animated.spring(scale, { toValue: 1, useNativeDriver: true, friction: 5 }),
    ]);
    pulse.start();
  }, [scale, visible]);

  return (
    <Modal visible={visible} transparent animationType="fade" accessibilityViewIsModal>
      <View style={styles.backdrop}>
        <Animated.View
          style={[styles.card, { backgroundColor: colors.surface, borderColor: colors.brand, transform: [{ scale }] }]}
        >
          <Text style={styles.star}>★</Text>
          <Text style={[styles.title, { color: colors.brand }]}>{preschoolT("celebration", language)}</Text>
          <Text style={[styles.body, { color: colors.text }]}>{preschoolT("starAward", language)}</Text>
          <Focusable accessibilityRole="button" onPress={onDone} style={[styles.button, { backgroundColor: colors.brand }]}>
            <Text style={[styles.buttonLabel, { color: colors.background }]}>{preschoolT("done", language)}</Text>
          </Focusable>
        </Animated.View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: "rgba(0,0,0,0.45)",
    alignItems: "center",
    justifyContent: "center",
    padding: 24,
  },
  card: {
    width: "100%",
    maxWidth: 360,
    borderRadius: 24,
    borderWidth: 4,
    padding: 24,
    alignItems: "center",
    gap: 12,
  },
  star: {
    fontSize: 72,
    color: "#F59E0B",
  },
  title: {
    fontSize: 28,
    fontWeight: "800",
  },
  body: {
    fontSize: 18,
    textAlign: "center",
  },
  button: {
    marginTop: 8,
    borderRadius: 16,
    paddingHorizontal: 24,
  },
  buttonLabel: {
    fontSize: 20,
    fontWeight: "700",
  },
});
