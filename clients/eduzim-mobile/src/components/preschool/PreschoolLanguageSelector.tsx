import { StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import { PRESCHOOL_LANGUAGES, type PreschoolLanguage } from "@/lib/preschool/languages";
import { preschoolT } from "@/lib/preschool/messages";
import { setPreschoolLanguage } from "@/lib/preschool/store";
import { useTheme } from "@/theme";

const LABELS: Record<PreschoolLanguage, string> = {
  en: "English",
  sn: "ChiShona",
  nd: "IsiNdebele",
};

type PreschoolLanguageSelectorProps = {
  language: PreschoolLanguage;
};

export function PreschoolLanguageSelector({ language }: PreschoolLanguageSelectorProps) {
  const { colors } = useTheme();

  return (
    <View accessibilityLabel={preschoolT("language", language)} style={styles.row}>
      {PRESCHOOL_LANGUAGES.map((item) => {
        const selected = item === language;
        return (
          <Focusable
            key={item}
            accessibilityRole="button"
            accessibilityState={{ selected }}
            accessibilityLabel={LABELS[item]}
            onPress={() => setPreschoolLanguage(item)}
            style={[
              styles.chip,
              {
                borderColor: selected ? colors.brand : colors.border,
                backgroundColor: selected ? colors.brand : colors.surface,
              },
            ]}
          >
            <Text style={{ color: selected ? colors.background : colors.text, fontWeight: "700", fontSize: 16 }}>
              {LABELS[item]}
            </Text>
          </Focusable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
  },
  chip: {
    borderWidth: 2,
    borderRadius: 999,
    paddingHorizontal: 14,
  },
});
