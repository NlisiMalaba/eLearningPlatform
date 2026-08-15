import { Text, View } from "react-native";
import { useConnectivityStore } from "@/lib/offline/connectivityStore";

export function OfflineBanner() {
  const { online } = useConnectivityStore();
  if (online) {
    return null;
  }

  return (
    <View
      accessibilityRole="alert"
      style={{
        backgroundColor: "#422006",
        paddingHorizontal: 16,
        paddingVertical: 12,
      }}
    >
      <Text style={{ color: "#FEF3C7", fontSize: 16 }}>
        You are offline. Progress will sync when connectivity is restored.
      </Text>
    </View>
  );
}
