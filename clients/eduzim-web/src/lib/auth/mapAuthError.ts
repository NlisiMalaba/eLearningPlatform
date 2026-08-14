import { ApiError, isNetworkError } from "@/lib/api/errors";
import type { MessageKey } from "@/lib/i18n/messages";

export function loginErrorKey(error: unknown): MessageKey {
  if (isNetworkError(error)) {
    return "auth.error.network";
  }

  if (error instanceof ApiError) {
    const detail = error.message.toLowerCase();
    if (error.status === 403 && detail.includes("lock")) {
      return "auth.error.locked";
    }

    if (error.status === 403) {
      return "auth.error.unverified";
    }

    if (error.status === 401) {
      return "auth.error.invalidCredentials";
    }
  }

  return "auth.error.generic";
}

export function registerErrorKey(error: unknown): MessageKey {
  if (isNetworkError(error)) {
    return "auth.error.network";
  }

  if (error instanceof ApiError && error.status === 409) {
    return "auth.error.conflict";
  }

  return "auth.error.generic";
}
