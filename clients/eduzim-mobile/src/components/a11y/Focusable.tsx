import { useCallback, useEffect, useId, useRef, type ComponentRef, type ReactNode } from "react";
import { Pressable, type PressableProps } from "react-native";
import { registerSwitchTarget, useSwitchAccess } from "@/lib/accessibility/switchAccess";
import { useTheme } from "@/theme";

type FocusableProps = PressableProps & {
  children?: ReactNode;
};

export function Focusable({
  accessibilityRole = "button",
  onPress,
  style,
  children,
  ...props
}: FocusableProps) {
  const id = useId();
  const ref = useRef<ComponentRef<typeof Pressable>>(null);
  const { colors } = useTheme();
  const { focusedId } = useSwitchAccess();
  const focused = focusedId === id;

  const activate = useCallback(() => {
    if (typeof onPress === "function") {
      onPress({ nativeEvent: {} } as Parameters<NonNullable<PressableProps["onPress"]>>[0]);
    }
  }, [onPress]);

  useEffect(() => {
    return registerSwitchTarget({
      id,
      activate,
      getNode: () => ref.current,
    });
  }, [activate, id]);

  return (
    <Pressable
      ref={ref}
      accessible
      focusable
      importantForAccessibility="yes"
      accessibilityRole={accessibilityRole}
      onPress={onPress}
      {...props}
      style={(state) => [
        { minHeight: 44, minWidth: 44, justifyContent: "center" },
        focused ? { borderWidth: 3, borderColor: colors.focusRing } : null,
        typeof style === "function" ? style(state) : style,
      ]}
    >
      {children}
    </Pressable>
  );
}
