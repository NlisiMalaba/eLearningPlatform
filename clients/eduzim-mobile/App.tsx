import { StatusBar } from "expo-status-bar";
import { useEffect, useState } from "react";
import { ActivityIndicator, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { AccessibilitySettings } from "@/components/a11y/AccessibilitySettings";
import { SwitchAccessHost } from "@/components/a11y/SwitchAccessHost";
import { OfflineBanner } from "@/components/OfflineBanner";
import { OfflineSyncManager } from "@/components/OfflineSyncManager";
import { Focusable } from "@/components/a11y/Focusable";
import { ModuleListScreen } from "@/components/learning/ModuleListScreen";
import { installExpoSpeechEngine } from "@/lib/accessibility/expoSpeech";
import {
  hydrateAccessibilityPreferences,
  persistAccessibilityPreferences,
} from "@/lib/accessibility/persistence";
import { useReadableSection } from "@/lib/accessibility/useReadableSection";
import { configureOfflineQueue } from "@/lib/offline/queue";
import { createSqliteQueueStore } from "@/lib/offline/sqliteQueueStore";
import { hydratePreschoolPreferences, persistPreschoolPreferences } from "@/lib/preschool/persistence";
import { preschoolT } from "@/lib/preschool/messages";
import { usePreschoolStore } from "@/lib/preschool/store";
import { LearningScreen } from "@/screens/LearningScreen";
import { PreschoolHomeScreen } from "@/screens/PreschoolHomeScreen";
import { t } from "@/lib/i18n/t";
import { useTheme } from "@/theme";

export default function App() {
  const { colors, highContrast } = useTheme();
  const { language } = usePreschoolStore();
  const styles = makeStyles(colors);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [moduleId, setModuleId] = useState<string | null>(readConfiguredModuleId());
  const [preschool, setPreschool] = useState(readConfiguredPreschool());
  const homeText = `${t("app.home.title")} ${t("app.home.subtitle")}`;
  useReadableSection("home", moduleId || preschool ? "" : homeText);

  useEffect(() => {
    installExpoSpeechEngine();
    const stopA11y = persistAccessibilityPreferences();
    const stopPreschool = persistPreschoolPreferences();
    let cancelled = false;
    void Promise.all([
      createSqliteQueueStore(),
      hydrateAccessibilityPreferences(),
      hydratePreschoolPreferences(),
    ])
      .then(([store]) => {
        if (cancelled) {
          return;
        }

        configureOfflineQueue(store);
        setReady(true);
      })
      .catch((reason: unknown) => {
        if (!cancelled) {
          setError(reason instanceof Error ? reason.message : "Offline storage could not be opened.");
        }
      });

    return () => {
      cancelled = true;
      stopA11y();
      stopPreschool();
    };
  }, []);

  if (error) {
    return (
      <SafeAreaView style={styles.safe}>
        <Text style={styles.error} accessibilityRole="alert">
          {error}
        </Text>
      </SafeAreaView>
    );
  }

  if (!ready) {
    return (
      <SafeAreaView style={styles.safe}>
        <ActivityIndicator color={colors.brand} />
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safe}>
      <StatusBar style={highContrast ? "light" : "dark"} />
      <SwitchAccessHost>
        <OfflineSyncManager />
        <OfflineBanner />
        <View style={styles.body} nativeID="main-content">
          <AccessibilitySettings />
          {preschool ? (
            <PreschoolHomeScreen onLeave={() => setPreschool(false)} />
          ) : moduleId ? (
            <LearningScreen moduleId={moduleId} onBack={() => setModuleId(null)} />
          ) : (
            <>
              <Text style={styles.title}>{t("app.home.title")}</Text>
              <Text style={styles.subtitle}>{t("app.home.subtitle")}</Text>
              <Focusable
                accessibilityRole="button"
                accessibilityLabel={preschoolT("openToddler", language)}
                onPress={() => setPreschool(true)}
                style={[styles.toddler, { borderColor: colors.brand }]}
              >
                <Text style={{ color: colors.brand, fontWeight: "700", fontSize: 16 }}>
                  {preschoolT("openToddler", language)}
                </Text>
              </Focusable>
              <ModuleListScreen onOpenModule={setModuleId} />
            </>
          )}
        </View>
      </SwitchAccessHost>
    </SafeAreaView>
  );
}

function readConfiguredModuleId(): string | null {
  const configured = process.env.EXPO_PUBLIC_MODULE_ID?.trim();
  return configured && configured.length > 0 ? configured : null;
}

function readConfiguredPreschool(): boolean {
  return process.env.EXPO_PUBLIC_TIER?.trim().toLowerCase() === "preschool";
}

function makeStyles(colors: ReturnType<typeof useTheme>["colors"]) {
  return StyleSheet.create({
    safe: {
      flex: 1,
      backgroundColor: colors.background,
    },
    body: {
      flex: 1,
      paddingHorizontal: 20,
      paddingTop: 16,
      gap: 12,
    },
    title: {
      fontSize: 28,
      fontWeight: "600",
      color: colors.brand,
    },
    subtitle: {
      fontSize: 16,
      color: colors.muted,
      maxWidth: 420,
    },
    toddler: {
      borderWidth: 2,
      borderRadius: 12,
      paddingHorizontal: 12,
    },
    error: {
      margin: 20,
      color: colors.danger,
      fontSize: 16,
    },
  });
}
