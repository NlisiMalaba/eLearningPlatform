import AsyncStorage from "@react-native-async-storage/async-storage";
import {
  ACCESSIBILITY_STORAGE_KEY,
  applyPersistedAccessibility,
  getAccessibilitySnapshot,
  subscribeAccessibility,
  toPersistedAccessibility,
} from "@/lib/accessibility/store";

export async function hydrateAccessibilityPreferences(): Promise<void> {
  try {
    const raw = await AsyncStorage.getItem(ACCESSIBILITY_STORAGE_KEY);
    if (!raw) {
      return;
    }

    applyPersistedAccessibility(JSON.parse(raw) as unknown);
  } catch {
    // Keep defaults when storage is unavailable or corrupt.
  }
}

export function persistAccessibilityPreferences(): () => void {
  return subscribeAccessibility(() => {
    const payload = JSON.stringify(toPersistedAccessibility(getAccessibilitySnapshot()));
    void AsyncStorage.setItem(ACCESSIBILITY_STORAGE_KEY, payload);
  });
}
