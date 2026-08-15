import { getServerApiBaseUrl } from "@/lib/config";
import type { TokenPair } from "@/lib/auth/types";

type UpstreamTokenPairJson = {
  accessToken?: string;
  accessTokenExpiresAt?: string;
  refreshToken?: string;
  refreshTokenExpiresAt?: string;
};

export type UpstreamResult<T> = {
  status: number;
  body: T | null;
  detail: string;
  location: string | null;
};

export async function postUpstreamJson(
  path: string,
  body: unknown,
): Promise<UpstreamResult<unknown>> {
  return sendUpstream(path, {
    method: "POST",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });
}

export async function getUpstream(path: string): Promise<UpstreamResult<unknown>> {
  return sendUpstream(path, {
    method: "GET",
    headers: { Accept: "application/json" },
    redirect: "manual",
  });
}

export function parseTokenPair(body: unknown): TokenPair | null {
  if (!body || typeof body !== "object") {
    return null;
  }

  const json = body as UpstreamTokenPairJson;
  if (!json.accessToken || !json.refreshToken || !json.accessTokenExpiresAt || !json.refreshTokenExpiresAt) {
    return null;
  }

  const accessTokenExpiresAt = new Date(json.accessTokenExpiresAt);
  const refreshTokenExpiresAt = new Date(json.refreshTokenExpiresAt);
  if (Number.isNaN(accessTokenExpiresAt.getTime()) || Number.isNaN(refreshTokenExpiresAt.getTime())) {
    return null;
  }

  return {
    accessToken: json.accessToken,
    refreshToken: json.refreshToken,
    accessTokenExpiresAt,
    refreshTokenExpiresAt,
  };
}

export function problemFromUpstream(result: UpstreamResult<unknown>, fallback: string): {
  type: string;
  title: string;
  status: number;
  detail: string;
} {
  const body = result.body;
  if (body && typeof body === "object" && "detail" in body && typeof body.detail === "string") {
    const title =
      "title" in body && typeof body.title === "string" ? body.title : fallback;
    return {
      type:
        "type" in body && typeof body.type === "string"
          ? body.type
          : "https://eduzim.co.zw/errors/auth",
      title,
      status: result.status,
      detail: body.detail,
    };
  }

  return {
    type: "https://eduzim.co.zw/errors/auth",
    title: fallback,
    status: result.status,
    detail: result.detail || fallback,
  };
}

async function sendUpstream(path: string, init: RequestInit): Promise<UpstreamResult<unknown>> {
  const url = `${getServerApiBaseUrl()}${path}`;
  let response: Response;
  try {
    response = await fetch(url, { ...init, cache: "no-store" });
  } catch {
    return {
      status: 503,
      body: null,
      detail: "The EduZim API could not be reached.",
      location: null,
    };
  }

  const location = response.headers.get("location");
  let body: unknown = null;
  const text = await response.text();
  if (text) {
    try {
      body = JSON.parse(text) as unknown;
    } catch {
      body = null;
    }
  }

  return {
    status: response.status,
    body,
    detail: readDetail(body, response.statusText),
    location,
  };
}

function readDetail(body: unknown, fallback: string): string {
  if (body && typeof body === "object" && "detail" in body && typeof body.detail === "string") {
    return body.detail;
  }

  return fallback;
}
