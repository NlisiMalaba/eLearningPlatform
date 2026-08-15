import { useEffect, useMemo, useState } from "react";
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from "react-native";
import { ContentItemPlayer } from "@/components/learning/ContentItemPlayer";
import { getModule } from "@/lib/content/contentService";
import type { ModuleContentItem, ModuleDetail } from "@/lib/content/types";
import { setLearningContext } from "@/lib/learning/context";
import { t } from "@/lib/i18n/t";
import { colors } from "@/theme";

type ModuleViewerProps = {
  moduleId: string;
  initialContentItemId?: string;
};

export function ModuleViewer({ moduleId, initialContentItemId }: ModuleViewerProps) {
  const [moduleDetail, setModuleDetail] = useState<ModuleDetail | null>(null);
  const [error, setError] = useState(false);
  const [loading, setLoading] = useState(true);
  const [selectedId, setSelectedId] = useState<string | undefined>(initialContentItemId);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(false);
    setLearningContext({ moduleId, inAssessment: false });

    void getModule(moduleId)
      .then((detail) => {
        if (!cancelled) {
          setModuleDetail(detail);
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
  }, [moduleId]);

  const items = moduleDetail?.contentItems ?? [];
  const activeId = useMemo(() => resolveActiveItemId(items, selectedId), [items, selectedId]);

  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} />
        <Text style={styles.muted}>{t("module.loading")}</Text>
      </View>
    );
  }

  if (error || !moduleDetail) {
    return (
      <Text accessibilityRole="alert" style={styles.error}>
        {t("module.error")}
      </Text>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.body}>
      <ModuleHeading title={moduleDetail.title} subject={moduleDetail.subject} />
      {items.length === 0 ? (
        <Text style={styles.muted}>{t("module.empty")}</Text>
      ) : (
        <>
          <View accessibilityLabel={t("module.items")} style={styles.nav}>
            {items.map((item) => {
              const current = item.contentItemId === activeId;
              return (
                <Pressable
                  key={item.contentItemId}
                  accessibilityRole="button"
                  accessibilityState={{ selected: current }}
                  accessibilityLabel={`${t("module.item")}: ${item.title}`}
                  onPress={() => setSelectedId(item.contentItemId)}
                  style={[styles.item, current ? styles.itemActive : null]}
                >
                  <Text style={[styles.itemLabel, current ? styles.itemLabelActive : null]}>
                    {item.title}
                  </Text>
                </Pressable>
              );
            })}
          </View>
          {activeId ? <ContentItemPlayer contentItemId={activeId} moduleId={moduleId} /> : null}
        </>
      )}
    </ScrollView>
  );
}

function ModuleHeading({ title, subject }: { title: string; subject: string }) {
  return (
    <View>
      <Text style={styles.subject}>{subject}</Text>
      <Text style={styles.title}>{title}</Text>
    </View>
  );
}

function resolveActiveItemId(
  items: readonly ModuleContentItem[],
  selectedId: string | undefined,
): string | undefined {
  if (selectedId && items.some((item) => item.contentItemId === selectedId)) {
    return selectedId;
  }

  return items[0]?.contentItemId;
}

const styles = StyleSheet.create({
  body: {
    gap: 16,
    paddingBottom: 120,
  },
  centered: {
    paddingVertical: 24,
    gap: 8,
  },
  subject: {
    color: colors.brand,
    fontSize: 14,
    fontWeight: "600",
  },
  title: {
    marginTop: 4,
    fontSize: 24,
    fontWeight: "600",
    color: colors.text,
  },
  nav: {
    gap: 8,
  },
  item: {
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 12,
    minHeight: 44,
  },
  itemActive: {
    borderColor: colors.brand,
    backgroundColor: colors.brandSoft,
  },
  itemLabel: {
    fontSize: 14,
    color: colors.text,
  },
  itemLabelActive: {
    fontWeight: "600",
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
