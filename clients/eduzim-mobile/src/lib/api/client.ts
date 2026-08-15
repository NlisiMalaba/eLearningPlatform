import { getApiBaseUrl } from "@/lib/config";
import { ApiError, NetworkError } from "@/lib/api/errors";
import { getAccessToken } from "@/lib/auth/session";

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getAccessToken();
  let response: Response;
  try {
    response = await fetch(`${getApiBaseUrl()}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...init?.headers,
      },
    });
  } catch (error: unknown) {
    throw new NetworkError(error instanceof Error ? error.message : "Network request failed");
  }

  if (!response.ok) {
    throw new ApiError(response.status, response.statusText || `Request failed with status ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
