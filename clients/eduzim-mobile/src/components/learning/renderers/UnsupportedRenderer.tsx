import { Text } from "react-native";
import { t } from "@/lib/i18n/t";
import { colors } from "@/theme";

export function UnsupportedRenderer() {
  return <Text style={{ color: colors.muted, fontSize: 14 }}>{t("content.unsupported")}</Text>;
}
