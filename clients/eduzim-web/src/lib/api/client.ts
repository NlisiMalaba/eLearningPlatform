import { getClientApiBaseUrl } from "@/lib/config";
import { ApiError, isBrowserOffline, NetworkError } from "@/lib/api/errors";
import { refreshSession } from "@/lib/auth/authService";
import { isAuthPath } from "@/lib/auth/routes";
import { refreshSessionOnce } from "@/lib/auth/refreshOnce";
import { notifySessionExpired } from "@/lib/auth/sessionEvents";
import { safeRedirectPath } from "@/lib/auth/safeRedirect";

type JsonValue = Record<string, unknown> | unknown[] | string | number | boolean | null;

type ApiFetchInit = RequestInit & {
  skipAuthRefresh?: boolean;
};

export async function apiFetch<T>(path: string, init?: ApiFetchInit): Promise<T> {
  if (isBrowserOffline()) {
    throw new NetworkError("Offline");
  }

  const response = await send(path, init);

  if (response.status === 401) {
    if (!init?.skipAuthRefresh) {
      const refreshed = await refreshSessionOnce(() => refreshSession());
      if (refreshed) {
        return apiFetch<T>(path, { ...init, skipAuthRefresh: true });
      }
    }

    redirectToLogin();
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readErrorDetail(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function send(path: string, init?: RequestInit): Promise<Response> {
  const url = `${getClientApiBaseUrl()}${path}`;
  try {
    return await fetch(url, {
      ...init,
      credentials: "include",
      headers: {
        Accept: "application/json",
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });
  } catch (error: unknown) {
    if (error instanceof NetworkError) {
      throw error;
    }

    throw new NetworkError(error instanceof Error ? error.message : "Network request failed");
  }
}

function redirectToLogin(): void {
  if (typeof window === "undefined") {
    return;
  }

  const pathname = window.location.pathname;
  if (isAuthPath(pathname)) {
    return;
  }

  notifySessionExpired(safeRedirectPath(pathname));
}

async function readErrorDetail(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as JsonValue;
    if (body && typeof body === "object" && "detail" in body && typeof body.detail === "string") {
      return body.detail;
    }
  } catch {
    // Fall through to status text when the body is not JSON.
  }

  return response.statusText || `Request failed with status ${response.status}`;
}
