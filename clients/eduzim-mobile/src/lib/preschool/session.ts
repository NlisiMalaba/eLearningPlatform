/** Matches `PreschoolSessionRules` on the API (requirements 3.6 and 3.7). */
export const REST_PROMPT_AFTER_MS = 20 * 60 * 1000;
export const INACTIVITY_PAUSE_AFTER_MS = 60 * 1000;

export type PreschoolSessionClock = {
  segmentStartedAt: number;
  lastInteractionAt: number;
  paused: boolean;
  restPromptRequired: boolean;
};

export function createPreschoolSessionClock(now: number): PreschoolSessionClock {
  return {
    segmentStartedAt: now,
    lastInteractionAt: now,
    paused: false,
    restPromptRequired: false,
  };
}

export function continuousInteractionMs(clock: PreschoolSessionClock, now: number): number {
  return Math.max(0, now - clock.segmentStartedAt);
}

export function idleMs(clock: PreschoolSessionClock, now: number): number {
  return Math.max(0, now - clock.lastInteractionAt);
}

export function shouldPromptRest(clock: PreschoolSessionClock, now: number): boolean {
  return continuousInteractionMs(clock, now) >= REST_PROMPT_AFTER_MS;
}

export function shouldPauseForInactivity(clock: PreschoolSessionClock, now: number): boolean {
  return !clock.paused && idleMs(clock, now) >= INACTIVITY_PAUSE_AFTER_MS;
}

export function recordPreschoolInteraction(
  clock: PreschoolSessionClock,
  now: number,
): PreschoolSessionClock {
  const next: PreschoolSessionClock = {
    ...clock,
    lastInteractionAt: now,
  };
  if (next.paused || next.restPromptRequired) {
    return next;
  }

  if (shouldPromptRest(next, now)) {
    return { ...next, restPromptRequired: true };
  }

  return next;
}

export function evaluatePreschoolSession(
  clock: PreschoolSessionClock,
  now: number,
): PreschoolSessionClock {
  if (clock.paused || clock.restPromptRequired) {
    return clock;
  }

  if (shouldPromptRest(clock, now)) {
    return { ...clock, restPromptRequired: true };
  }

  if (shouldPauseForInactivity(clock, now)) {
    return { ...clock, paused: true };
  }

  return clock;
}

export function acknowledgePreschoolRest(
  clock: PreschoolSessionClock,
  now: number,
): PreschoolSessionClock {
  return {
    segmentStartedAt: now,
    lastInteractionAt: now,
    paused: false,
    restPromptRequired: false,
  };
}

export function resumePreschoolSession(
  clock: PreschoolSessionClock,
  now: number,
): PreschoolSessionClock {
  return {
    segmentStartedAt: now,
    lastInteractionAt: now,
    paused: false,
    restPromptRequired: false,
  };
}
