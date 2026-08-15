import { StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import { ModuleViewer } from "@/components/learning/ModuleViewer";
import { ZimBotWidget } from "@/components/zimbot/ZimBotWidget";
import { setLearningContext } from "@/lib/learning/context";
import { t } from "@/lib/i18n/t";
import { useTheme } from "@/theme";

type LearningScreenProps = {
  moduleId: string;
  onBack: () => void;
};

export function LearningScreen({ moduleId, onBack }: LearningScreenProps) {
  const { colors } = useTheme();

  return (
    <View style={styles.root}>
      <Focusable
        accessibilityRole="button"
        accessibilityLabel={t("module.back")}
        onPress={() => {
          setLearningContext({ inAssessment: false });
          onBack();
        }}
        style={styles.back}
      >
        <Text style={[styles.backLabel, { color: colors.brand }]}>{t("module.back")}</Text>
      </Focusable>
      <ModuleViewer moduleId={moduleId} />
      <ZimBotWidget />
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
  },
  back: {
    marginBottom: 8,
  },
  backLabel: {
    fontWeight: "600",
    fontSize: 14,
  },
});
