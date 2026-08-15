import { cookies } from "next/headers";
import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/cookies";
import { getServerApiBaseUrl } from "@/lib/config";
import { ApiError, NetworkError } from "@/lib/api/errors";

export async function serverApiFetch(path: string): Promise<Record<string, unknown>> {
  const cookieStore = await cookies();
  const token = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;
  let response: Response;
  try {
    response = await fetch(`${getServerApiBaseUrl()}${path}`, {
      headers: {
        Accept: "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      cache: "no-store",
    });
  } catch (error: unknown) {
    throw new NetworkError(error instanceof Error ? error.message : "Network request failed");
  }

  if (!response.ok) {
    throw new ApiError(response.status, response.statusText || "Request failed");
  }

  return (await response.json()) as Record<string, unknown>;
}
