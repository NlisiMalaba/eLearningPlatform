import { useAccessibilityStore } from "@/lib/accessibility/store";

export type ThemeColors = {
  brand: string;
  brandSoft: string;
  background: string;
  surface: string;
  text: string;
  muted: string;
  border: string;
  danger: string;
  warningBg: string;
  warningBorder: string;
  warningText: string;
  focusRing: string;
};

export const colors: ThemeColors = {
  brand: "#0B6E4F",
  brandSoft: "#0B6E4F1A",
  background: "#FAFAF9",
  surface: "#FFFFFF",
  text: "#18181B",
  muted: "#52525B",
  border: "#E4E4E7",
  danger: "#B91C1C",
  warningBg: "#FFFBEB",
  warningBorder: "#FDE68A",
  warningText: "#78350F",
  focusRing: "#0B6E4F",
};

export const highContrastColors: ThemeColors = {
  brand: "#FDE047",
  brandSoft: "#1A1500",
  background: "#000000",
  surface: "#000000",
  text: "#FFFFFF",
  muted: "#FFFFFF",
  border: "#FFFFFF",
  danger: "#FF6B6B",
  warningBg: "#000000",
  warningBorder: "#FDE047",
  warningText: "#FDE047",
  focusRing: "#FDE047",
};

export function paletteForContrast(highContrast: boolean): ThemeColors {
  return highContrast ? highContrastColors : colors;
}

export function useTheme(): { colors: ThemeColors; highContrast: boolean } {
  const { highContrast } = useAccessibilityStore();
  return {
    highContrast,
    colors: paletteForContrast(highContrast),
  };
}
