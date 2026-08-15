import { useEffect, useRef } from "react";
import { Animated, Easing, StyleSheet, Text, View } from "react-native";
import { preschoolT } from "@/lib/preschool/messages";
import type { PreschoolLanguage } from "@/lib/preschool/languages";
import { useTheme } from "@/theme";

type AnimatedCharacterProps = {
  language: PreschoolLanguage;
  excited?: boolean;
  demonstrating?: string | null;
};

export function AnimatedCharacter({ language, excited = false, demonstrating }: AnimatedCharacterProps) {
  const { colors } = useTheme();
  const bounce = useRef(new Animated.Value(0)).current;
  const spin = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(bounce, {
          toValue: 1,
          duration: excited ? 280 : 700,
          easing: Easing.inOut(Easing.quad),
          useNativeDriver: true,
        }),
        Animated.timing(bounce, {
          toValue: 0,
          duration: excited ? 280 : 700,
          easing: Easing.inOut(Easing.quad),
          useNativeDriver: true,
        }),
      ]),
    );
    loop.start();
    return () => loop.stop();
  }, [bounce, excited]);

  useEffect(() => {
    if (!demonstrating) {
      spin.setValue(0);
      return;
    }

    const wave = Animated.sequence([
      Animated.timing(spin, { toValue: 1, duration: 180, useNativeDriver: true }),
      Animated.timing(spin, { toValue: -1, duration: 180, useNativeDriver: true }),
      Animated.timing(spin, { toValue: 0, duration: 180, useNativeDriver: true }),
    ]);
    wave.start();
  }, [demonstrating, spin]);

  const translateY = bounce.interpolate({ inputRange: [0, 1], outputRange: [0, -14] });
  const rotate = spin.interpolate({ inputRange: [-1, 1], outputRange: ["-12deg", "12deg"] });

  return (
    <View accessible accessibilityLabel={preschoolT("character", language)} style={styles.wrap}>
      <Animated.View
        style={[
          styles.body,
          { backgroundColor: colors.brand, transform: [{ translateY }, { rotate }] },
        ]}
      >
        <Text style={styles.face}>◡</Text>
        {demonstrating ? <Text style={styles.demo}>{demonstrating}</Text> : null}
      </Animated.View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: "center",
    minHeight: 140,
    justifyContent: "center",
  },
  body: {
    width: 120,
    height: 120,
    borderRadius: 60,
    alignItems: "center",
    justifyContent: "center",
  },
  face: {
    fontSize: 48,
    color: "#FFFFFF",
  },
  demo: {
    marginTop: 4,
    fontSize: 28,
    fontWeight: "800",
    color: "#FFFFFF",
  },
});
