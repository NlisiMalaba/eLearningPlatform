import { getClientApiBaseUrl } from "@/lib/config";
import { ApiError, isBrowserOffline, NetworkError } from "@/lib/api/errors";

type JsonValue = Record<string, unknown> | unknown[] | string | number | boolean | null;

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  if (isBrowserOffline()) {
    throw new NetworkError("Offline");
  }

  const url = `${getClientApiBaseUrl()}${path}`;

  let response: Response;
  try {
    response = await fetch(url, {
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

  if (!response.ok) {
    const detail = await readErrorDetail(response);
    throw new ApiError(response.status, detail);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
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
