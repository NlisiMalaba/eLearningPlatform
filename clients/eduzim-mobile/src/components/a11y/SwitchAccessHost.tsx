import type { ReactNode } from "react";
import { View, type NativeSyntheticEvent, type ViewProps } from "react-native";
import { applySwitchAction } from "@/lib/accessibility/switchAccess";
import { interpretSwitchKey } from "@/lib/accessibility/switchKeys";

type KeyEvent = NativeSyntheticEvent<{ key: string; shiftKey?: boolean }>;

type SwitchAccessHostProps = {
  children: ReactNode;
};

export function SwitchAccessHost({ children }: SwitchAccessHostProps) {
  const props: ViewProps & { onKeyDown?: (event: KeyEvent) => void } = {
    style: { flex: 1 },
    onKeyDown: (event: KeyEvent) => {
      const action = interpretSwitchKey(event.nativeEvent.key, Boolean(event.nativeEvent.shiftKey));
      if (!action) {
        return;
      }

      applySwitchAction(action);
    },
  };

  return <View {...(props as ViewProps)}>{children}</View>;
}
