import { useState } from "react";
import { ScrollView, StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import { AnimatedCharacter } from "@/components/preschool/AnimatedCharacter";
import { PreschoolLanguageSelector } from "@/components/preschool/PreschoolLanguageSelector";
import { PreschoolSessionGuard } from "@/components/preschool/PreschoolSessionGuard";
import { FOUNDATIONAL_CATEGORIES, type FoundationalCategory } from "@/lib/preschool/catalog";
import { preschoolT } from "@/lib/preschool/messages";
import { usePreschoolStore } from "@/lib/preschool/store";
import { PreschoolActivityScreen } from "@/screens/PreschoolActivityScreen";
import { useTheme } from "@/theme";

type PreschoolHomeScreenProps = {
  onLeave: () => void;
};

export function PreschoolHomeScreen({ onLeave }: PreschoolHomeScreenProps) {
  const { colors } = useTheme();
  const { language, stars } = usePreschoolStore();
  const [category, setCategory] = useState<FoundationalCategory | null>(null);

  return (
    <PreschoolSessionGuard language={language}>
      {category ? (
        <PreschoolActivityScreen category={category} language={language} onBack={() => setCategory(null)} />
      ) : (
        <ScrollView contentContainerStyle={styles.body}>
          <Focusable accessibilityRole="button" onPress={onLeave}>
            <Text style={{ color: colors.brand, fontSize: 16, fontWeight: "700" }}>
              {preschoolT("school", language)}
            </Text>
          </Focusable>
          <PreschoolLanguageSelector language={language} />
          <AnimatedCharacter language={language} />
          <Text style={[styles.title, { color: colors.brand }]}>{preschoolT("title", language)}</Text>
          <Text style={[styles.subtitle, { color: colors.muted }]}>{preschoolT("subtitle", language)}</Text>
          <Text style={[styles.stars, { color: colors.text }]}>
            ★ {preschoolT("stars", language)}: {stars}
          </Text>
          <View style={styles.grid}>
            {FOUNDATIONAL_CATEGORIES.map((item) => (
              <Focusable
                key={item}
                accessibilityRole="button"
                accessibilityLabel={preschoolT(item, language)}
                onPress={() => setCategory(item)}
                style={[styles.card, { backgroundColor: colors.surface, borderColor: colors.brand }]}
              >
                <Text style={[styles.cardLabel, { color: colors.text }]}>{preschoolT(item, language)}</Text>
              </Focusable>
            ))}
          </View>
        </ScrollView>
      )}
    </PreschoolSessionGuard>
  );
}

const styles = StyleSheet.create({
  body: {
    gap: 12,
    paddingBottom: 32,
  },
  title: {
    fontSize: 30,
    fontWeight: "800",
  },
  subtitle: {
    fontSize: 18,
    lineHeight: 26,
  },
  stars: {
    fontSize: 20,
    fontWeight: "700",
  },
  grid: {
    gap: 10,
  },
  card: {
    borderWidth: 2,
    borderRadius: 18,
    paddingHorizontal: 16,
    minHeight: 64,
  },
  cardLabel: {
    fontSize: 22,
    fontWeight: "700",
  },
});
