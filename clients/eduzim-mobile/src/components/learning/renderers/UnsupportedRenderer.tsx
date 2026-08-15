import { Text } from "react-native";
import { t } from "@/lib/i18n/t";
import { useTheme } from "@/theme";

export function UnsupportedRenderer() {
  const { colors } = useTheme();
  return <Text style={{ color: colors.muted, fontSize: 14 }}>{t("content.unsupported")}</Text>;
}
