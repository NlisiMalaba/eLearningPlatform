import AsyncStorage from "@react-native-async-storage/async-storage";
import {
  PRESCHOOL_STORAGE_KEY,
  applyPersistedPreschool,
  getPreschoolSnapshot,
  subscribePreschool,
  toPersistedPreschool,
} from "@/lib/preschool/store";

export async function hydratePreschoolPreferences(): Promise<void> {
  try {
    const raw = await AsyncStorage.getItem(PRESCHOOL_STORAGE_KEY);
    if (!raw) {
      return;
    }

    applyPersistedPreschool(JSON.parse(raw) as unknown);
  } catch {
    // Keep defaults when storage is unavailable.
  }
}

export function persistPreschoolPreferences(): () => void {
  return subscribePreschool(() => {
    void AsyncStorage.setItem(PRESCHOOL_STORAGE_KEY, JSON.stringify(toPersistedPreschool(getPreschoolSnapshot())));
  });
}
