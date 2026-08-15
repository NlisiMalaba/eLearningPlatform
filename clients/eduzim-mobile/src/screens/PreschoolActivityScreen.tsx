import { useMemo, useState } from "react";
import { ScrollView, StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import { AnimatedCharacter } from "@/components/preschool/AnimatedCharacter";
import { CelebrationOverlay } from "@/components/preschool/CelebrationOverlay";
import { speakText } from "@/lib/accessibility/speech";
import { setSpeaking } from "@/lib/accessibility/store";
import { itemsForCategory, type FoundationalCategory, type PreschoolItem } from "@/lib/preschool/catalog";
import type { PreschoolLanguage } from "@/lib/preschool/languages";
import { preschoolT } from "@/lib/preschool/messages";
import { awardPreschoolStar } from "@/lib/preschool/store";
import { useTheme } from "@/theme";

type PreschoolActivityScreenProps = {
  category: FoundationalCategory;
  language: PreschoolLanguage;
  onBack: () => void;
};

export function PreschoolActivityScreen({ category, language, onBack }: PreschoolActivityScreenProps) {
  const { colors } = useTheme();
  const items = useMemo(() => itemsForCategory(category), [category]);
  const [seen, setSeen] = useState<Set<string>>(new Set());
  const [demonstrating, setDemonstrating] = useState<string | null>(null);
  const [celebrate, setCelebrate] = useState(false);

  function onTap(item: PreschoolItem): void {
    setDemonstrating(item.glyph);
    speakText(item.speak[language], language, {
      onEnd: () => setSpeaking(false),
      onError: () => setSpeaking(false),
    });
    setSeen((current) => {
      const next = new Set(current);
      next.add(item.id);
      if (next.size === items.length && current.size < items.length) {
        awardPreschoolStar();
        setCelebrate(true);
      }
      return next;
    });
  }

  return (
    <View style={styles.root}>
      <Focusable accessibilityRole="button" onPress={onBack}>
        <Text style={{ color: colors.brand, fontSize: 18, fontWeight: "700" }}>{preschoolT("back", language)}</Text>
      </Focusable>
      <Text style={[styles.heading, { color: colors.text }]}>{preschoolT(category, language)}</Text>
      <AnimatedCharacter language={language} excited={Boolean(demonstrating)} demonstrating={demonstrating} />
      <ScrollView contentContainerStyle={styles.grid}>
        {items.map((item) => (
          <Focusable
            key={item.id}
            accessibilityRole="button"
            accessibilityLabel={item.speak[language]}
            onPress={() => onTap(item)}
            style={[
              styles.tile,
              {
                borderColor: seen.has(item.id) ? colors.brand : colors.border,
                backgroundColor: colors.surface,
              },
            ]}
          >
            <Text style={[styles.glyph, { color: colors.text }]}>{item.glyph}</Text>
          </Focusable>
        ))}
      </ScrollView>
      <CelebrationOverlay visible={celebrate} language={language} onDone={() => setCelebrate(false)} />
    </View>
  );
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
    gap: 12,
  },
  heading: {
    fontSize: 28,
    fontWeight: "800",
  },
  grid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 10,
    paddingBottom: 32,
  },
  tile: {
    width: 72,
    height: 72,
    borderWidth: 2,
    borderRadius: 16,
    alignItems: "center",
  },
  glyph: {
    fontSize: 28,
    fontWeight: "800",
    textAlign: "center",
  },
});
