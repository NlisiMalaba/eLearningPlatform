import { StatusBar } from "expo-status-bar";
import { useEffect, useState } from "react";
import { ActivityIndicator, SafeAreaView, StyleSheet, Text, View } from "react-native";
import { OfflineBanner } from "@/components/OfflineBanner";
import { OfflineSyncManager } from "@/components/OfflineSyncManager";
import { ModuleListScreen } from "@/components/learning/ModuleListScreen";
import { LearningScreen } from "@/screens/LearningScreen";
import { configureOfflineQueue } from "@/lib/offline/queue";
import { createSqliteQueueStore } from "@/lib/offline/sqliteQueueStore";
import { colors } from "@/theme";

export default function App() {
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [moduleId, setModuleId] = useState<string | null>(readConfiguredModuleId());

  useEffect(() => {
    let cancelled = false;
    void createSqliteQueueStore()
      .then((store) => {
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
      <StatusBar style="dark" />
      <OfflineSyncManager />
      <OfflineBanner />
      <View style={styles.body}>
        {moduleId ? (
          <LearningScreen moduleId={moduleId} onBack={() => setModuleId(null)} />
        ) : (
          <>
            <Text style={styles.title}>EduZim</Text>
            <Text style={styles.subtitle}>
              Continue your modules when you are ready. Progress is stored on this device while you are
              offline.
            </Text>
            <ModuleListScreen onOpenModule={setModuleId} />
          </>
        )}
      </View>
    </SafeAreaView>
  );
}

function readConfiguredModuleId(): string | null {
  const configured = process.env.EXPO_PUBLIC_MODULE_ID?.trim();
  return configured && configured.length > 0 ? configured : null;
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: colors.background,
  },
  body: {
    flex: 1,
    paddingHorizontal: 20,
    paddingTop: 24,
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
  error: {
    margin: 20,
    color: colors.danger,
    fontSize: 16,
  },
});
