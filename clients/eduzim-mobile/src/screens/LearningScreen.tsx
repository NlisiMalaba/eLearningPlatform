import { Pressable, StyleSheet, Text, View } from "react-native";
import { ModuleViewer } from "@/components/learning/ModuleViewer";
import { ZimBotWidget } from "@/components/zimbot/ZimBotWidget";
import { setLearningContext } from "@/lib/learning/context";
import { t } from "@/lib/i18n/t";
import { colors } from "@/theme";

type LearningScreenProps = {
  moduleId: string;
  onBack: () => void;
};

export function LearningScreen({ moduleId, onBack }: LearningScreenProps) {
  return (
    <View style={styles.root}>
      <Pressable
        accessibilityRole="button"
        onPress={() => {
          setLearningContext({ inAssessment: false });
          onBack();
        }}
        style={styles.back}
      >
        <Text style={styles.backLabel}>{t("module.back")}</Text>
      </Pressable>
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
    minHeight: 44,
    justifyContent: "center",
    marginBottom: 8,
  },
  backLabel: {
    color: colors.brand,
    fontWeight: "600",
    fontSize: 14,
  },
});
