import { Text, View } from "react-native";
import { useConnectivityStore } from "@/lib/offline/connectivityStore";
import { useTheme } from "@/theme";

export function OfflineBanner() {
  const { online } = useConnectivityStore();
  const { colors, highContrast } = useTheme();
  if (online) {
    return null;
  }

  return (
    <View
      accessibilityRole="alert"
      style={{
        backgroundColor: highContrast ? colors.warningBg : "#422006",
        borderBottomWidth: highContrast ? 2 : 0,
        borderBottomColor: colors.warningBorder,
        paddingHorizontal: 16,
        paddingVertical: 12,
      }}
    >
      <Text style={{ color: highContrast ? colors.warningText : "#FEF3C7", fontSize: 16 }}>
        You are offline. Progress will sync when connectivity is restored.
      </Text>
    </View>
  );
}
