import { Linking, Pressable, StyleSheet, Text, View } from "react-native";
import { WebView } from "react-native-webview";
import { t } from "@/lib/i18n/t";
import { colors } from "@/theme";

type PdfRendererProps = {
  src: string;
  title: string;
};

export function PdfRenderer({ src, title }: PdfRendererProps) {
  return (
    <View style={styles.stack}>
      <View
        style={styles.frame}
        accessibilityLabel={`${t("content.pdf.label")}: ${title}`}
      >
        <WebView source={{ uri: src }} originWhitelist={["*"]} style={styles.webview} />
      </View>
      <Pressable
        accessibilityRole="link"
        onPress={() => {
          void Linking.openURL(src);
        }}
      >
        <Text style={styles.link}>{t("content.pdf.open")}</Text>
      </Pressable>
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
    borderColor: colors.border,
    backgroundColor: colors.surface,
  },
  webview: {
    flex: 1,
  },
  link: {
    color: colors.brand,
    fontSize: 14,
    fontWeight: "600",
    textDecorationLine: "underline",
  },
});
