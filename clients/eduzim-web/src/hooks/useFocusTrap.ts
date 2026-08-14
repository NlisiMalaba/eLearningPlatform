"use client";

import { useEffect, type RefObject } from "react";

const FOCUSABLE =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

export function useFocusTrap(
  active: boolean,
  containerRef: RefObject<HTMLElement | null>,
  onEscape: () => void,
): void {
  useEffect(() => {
    if (!active) {
      return;
    }

    const root = containerRef.current;
    if (!root) {
      return;
    }

    const previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const items = focusableItems(root);
    items[0]?.focus();

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onEscape();
        return;
      }

      if (event.key !== "Tab") {
        return;
      }

      trapTab(event, root);
    };

    root.addEventListener("keydown", onKeyDown);
    return () => {
      root.removeEventListener("keydown", onKeyDown);
      previouslyFocused?.focus();
    };
  }, [active, containerRef, onEscape]);
}

function focusableItems(root: HTMLElement): HTMLElement[] {
  return [...root.querySelectorAll<HTMLElement>(FOCUSABLE)].filter(
    (element) => !element.hasAttribute("disabled") && element.tabIndex !== -1,
  );
}

function trapTab(event: KeyboardEvent, root: HTMLElement): void {
  const items = focusableItems(root);
  if (items.length === 0) {
    event.preventDefault();
    return;
  }

  const first = items[0];
  const last = items[items.length - 1];
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault();
    last.focus();
    return;
  }

  if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault();
    first.focus();
  }
}
