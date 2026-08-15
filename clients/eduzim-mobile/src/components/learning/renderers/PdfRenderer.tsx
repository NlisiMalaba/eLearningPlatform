import { Linking, StyleSheet, Text, View } from "react-native";
import { WebView } from "react-native-webview";
import { Focusable } from "@/components/a11y/Focusable";
import { t } from "@/lib/i18n/t";
import { useTheme } from "@/theme";

type PdfRendererProps = {
  src: string;
  title: string;
};

export function PdfRenderer({ src, title }: PdfRendererProps) {
  const { colors } = useTheme();

  return (
    <View style={styles.stack}>
      <View
        style={[styles.frame, { borderColor: colors.border, backgroundColor: colors.surface }]}
        accessibilityLabel={`${t("content.pdf.label")}: ${title}`}
        importantForAccessibility="no-hide-descendants"
      >
        <WebView source={{ uri: src }} originWhitelist={["*"]} style={styles.webview} />
      </View>
      <Focusable
        accessibilityRole="link"
        accessibilityLabel={t("content.pdf.open")}
        onPress={() => {
          void Linking.openURL(src);
        }}
      >
        <Text style={{ color: colors.brand, fontSize: 14, fontWeight: "600", textDecorationLine: "underline" }}>
          {t("content.pdf.open")}
        </Text>
      </Focusable>
    </View>
  );
}

const styles = StyleSheet.create({
  stack: {
    gap: 12,
  },
  frame: {
    height: 420,
    borderRadius: 12,
    overflow: "hidden",
    borderWidth: 1,
  },
  webview: {
    flex: 1,
  },
});
