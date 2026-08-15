import { useSyncExternalStore } from "react";

export type LearningContextSnapshot = {
  moduleId?: string;
  inAssessment: boolean;
};

const listeners = new Set<() => void>();

let snapshot: LearningContextSnapshot = {
  inAssessment: false,
};

function emit(): void {
  for (const listener of listeners) {
    listener();
  }
}

export function setLearningContext(next: LearningContextSnapshot): void {
  snapshot = next;
  emit();
}

export function getLearningContext(): LearningContextSnapshot {
  return snapshot;
}

export function subscribeLearningContext(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function useLearningContext(): LearningContextSnapshot {
  return useSyncExternalStore(subscribeLearningContext, getLearningContext, getLearningContext);
}
