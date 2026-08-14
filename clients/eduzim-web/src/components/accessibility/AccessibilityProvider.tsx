"use client";

import { useEffect } from "react";
import { useAccessibilityStore } from "@/lib/accessibility/store";

export function AccessibilityProvider() {
  const highContrast = useAccessibilityStore((state) => state.highContrast);
  const fontSize = useAccessibilityStore((state) => state.fontSize);

  useEffect(() => {
    const root = document.documentElement;
    root.dataset.contrast = highContrast ? "high" : "normal";
    root.dataset.fontSize = fontSize;
  }, [highContrast, fontSize]);

  return null;
}
