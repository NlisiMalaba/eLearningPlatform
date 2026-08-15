import { useCallback, useEffect, useState, type ReactNode } from "react";
import { View } from "react-native";
import { SessionPrompt } from "@/components/preschool/SessionPrompt";
import type { PreschoolLanguage } from "@/lib/preschool/languages";
import {
  acknowledgePreschoolRest,
  createPreschoolSessionClock,
  evaluatePreschoolSession,
  recordPreschoolInteraction,
  resumePreschoolSession,
  type PreschoolSessionClock,
} from "@/lib/preschool/session";

type PreschoolSessionGuardProps = {
  language: PreschoolLanguage;
  children: ReactNode;
};

export function PreschoolSessionGuard({ language, children }: PreschoolSessionGuardProps) {
  const [clock, setClock] = useState<PreschoolSessionClock>(() => createPreschoolSessionClock(Date.now()));

  useEffect(() => {
    const timer = setInterval(() => {
      setClock((current) => evaluatePreschoolSession(current, Date.now()));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const onTouch = useCallback(() => {
    setClock((current) => recordPreschoolInteraction(current, Date.now()));
  }, []);

  return (
    <View style={{ flex: 1 }} onTouchStart={onTouch}>
      {children}
      {clock.restPromptRequired ? (
        <SessionPrompt
          kind="rest"
          language={language}
          onContinue={() => setClock((current) => acknowledgePreschoolRest(current, Date.now()))}
        />
      ) : null}
      {clock.paused && !clock.restPromptRequired ? (
        <SessionPrompt
          kind="pause"
          language={language}
          onContinue={() => setClock((current) => resumePreschoolSession(current, Date.now()))}
        />
      ) : null}
    </View>
  );
}
