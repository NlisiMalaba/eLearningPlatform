import { useEffect, useState } from "react";
import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from "react-native";
import { Focusable } from "@/components/a11y/Focusable";
import { useReadableSection } from "@/lib/accessibility/useReadableSection";
import { listModules } from "@/lib/content/contentService";
import type { ModuleListItem } from "@/lib/content/types";
import { t } from "@/lib/i18n/t";
import { useTheme, type ThemeColors } from "@/theme";

type ModuleListScreenProps = {
  onOpenModule: (moduleId: string) => void;
};

export function ModuleListScreen({ onOpenModule }: ModuleListScreenProps) {
  const { colors } = useTheme();
  const styles = makeStyles(colors);
  const [items, setItems] = useState<ModuleListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  useReadableSection("modules", items.map((item) => `${item.subject} ${item.title}`).join(". "));

  useEffect(() => {
    let cancelled = false;
    void listModules()
      .then((modules) => {
        if (!cancelled) {
          setItems(modules);
          setLoading(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError(true);
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} />
        <Text style={styles.muted}>{t("modules.loading")}</Text>
      </View>
    );
  }

  if (error) {
    return (
      <Text accessibilityRole="alert" style={styles.error}>
        {t("modules.error")}
      </Text>
    );
  }

  if (items.length === 0) {
    return <Text style={styles.muted}>{t("modules.empty")}</Text>;
  }

  return (
    <ScrollView contentContainerStyle={styles.list}>
      {items.map((item) => (
        <Focusable
          key={item.id}
          accessibilityRole="button"
          accessibilityLabel={`${t("module.item")}: ${item.title}`}
          onPress={() => onOpenModule(item.id)}
          style={styles.card}
        >
          <Text style={styles.subject}>{item.subject}</Text>
          <Text style={styles.title}>{item.title}</Text>
        </Focusable>
      ))}
    </ScrollView>
  );
}

function makeStyles(colors: ThemeColors) {
  return StyleSheet.create({
    centered: {
      paddingVertical: 24,
      gap: 8,
    },
    list: {
      gap: 8,
      paddingBottom: 24,
    },
    card: {
      borderWidth: 1,
      borderColor: colors.border,
      backgroundColor: colors.surface,
      borderRadius: 12,
      padding: 16,
    },
    subject: {
      color: colors.brand,
      fontSize: 13,
      fontWeight: "600",
    },
    title: {
      marginTop: 4,
      fontSize: 16,
      fontWeight: "600",
      color: colors.text,
    },
    muted: {
      color: colors.muted,
      fontSize: 16,
    },
    error: {
      color: colors.danger,
      fontSize: 16,
    },
  });
}
