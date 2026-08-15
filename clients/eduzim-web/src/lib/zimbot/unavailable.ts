import { ApiError, isNetworkError } from "@/lib/api/errors";

export const ZIMBOT_UNAVAILABLE_REPLY =
  "ZimBot is temporarily unavailable. Please try again shortly. In the meantime, open the help resources in your current module or ask your teacher.";

export function isZimBotUnavailable(error: unknown, usedFallback: boolean): boolean {
  if (usedFallback) {
    return true;
  }

  if (isNetworkError(error)) {
    return true;
  }

  return error instanceof ApiError && (error.status === 503 || error.status === 0);
}
