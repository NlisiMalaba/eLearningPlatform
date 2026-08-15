import { ApiError, NetworkError } from "@/lib/api/errors";
import type { PublicRegisterRole, SessionInfo } from "@/lib/auth/types";

export async function login(email: string, password: string): Promise<SessionInfo> {
  return postJson<SessionInfo>("/api/auth/login", { email, password });
}

export async function registerAccount(input: {
  fullName: string;
  email: string;
  password: string;
  role: PublicRegisterRole;
}): Promise<{ userId: string | null }> {
  return postJson("/api/auth/register", input);
}

export async function refreshSession(): Promise<SessionInfo> {
  return postJson<SessionInfo>("/api/auth/refresh", {});
}

export async function loadSession(): Promise<SessionInfo> {
  const response = await fetch("/api/auth/session", {
    method: "GET",
    credentials: "include",
    cache: "no-store",
  });

  if (response.status === 401) {
    return { authenticated: false };
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  return (await response.json()) as SessionInfo;
}

export async function logout(): Promise<void> {
  const response = await fetch("/api/auth/logout", {
    method: "POST",
    credentials: "include",
  });

  if (!response.ok && response.status !== 204) {
    throw await toApiError(response);
  }
}

export async function verifyEmail(userId: string, token: string): Promise<void> {
  await postJson("/api/auth/verify-email", { userId, token });
}

export async function startSso(tenantId: string): Promise<{ redirectUrl: string }> {
  const response = await fetch(`/api/auth/sso/${encodeURIComponent(tenantId)}`, {
    method: "GET",
    credentials: "include",
    cache: "no-store",
  });

  if (!response.ok) {
    throw await toApiError(response);
  }

  const body = (await response.json()) as { redirectUrl?: string };
  if (!body.redirectUrl) {
    throw new ApiError(404, "SSO is not configured for this school");
  }

  return { redirectUrl: body.redirectUrl };
}

async function postJson<T>(path: string, body: unknown): Promise<T> {
  let response: Response;
  try {
    response = await fetch(path, {
      method: "POST",
      credentials: "include",
      headers: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
    });
  } catch (error: unknown) {
    throw new NetworkError(error instanceof Error ? error.message : "Network request failed");
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const body = (await response.json()) as { detail?: unknown };
    if (typeof body.detail === "string" && body.detail.length > 0) {
      return new ApiError(response.status, body.detail);
    }
  } catch {
    // Fall through to status text.
  }

  return new ApiError(response.status, response.statusText || "Request failed");
}
