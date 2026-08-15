export const SESSION_EXPIRED_EVENT = "eduzim:session-expired";

export function notifySessionExpired(fromPath: string): void {
  if (typeof window === "undefined") {
    return;
  }

  window.dispatchEvent(
    new CustomEvent(SESSION_EXPIRED_EVENT, { detail: { from: fromPath } }),
  );
}
