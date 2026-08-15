import { NextRequest, NextResponse } from "next/server";
import { applyAuthCookies } from "@/lib/auth/sessionCookies";
import { sessionInfoFromAccessToken } from "@/lib/auth/sessionInfo";
import { parseTokenPair, postUpstreamJson, problemFromUpstream } from "@/lib/auth/upstreamAuth";

export async function POST(request: NextRequest): Promise<NextResponse> {
  const payload = (await request.json().catch(() => null)) as {
    email?: unknown;
    password?: unknown;
  } | null;

  const email = typeof payload?.email === "string" ? payload.email.trim() : "";
  const password = typeof payload?.password === "string" ? payload.password : "";
  if (!email || !password) {
    return NextResponse.json(
      {
        type: "https://eduzim.co.zw/errors/validation",
        title: "Validation failed",
        status: 400,
        detail: "Email and password are required.",
      },
      { status: 400 },
    );
  }

  const upstream = await postUpstreamJson("/api/v1/auth/login", { email, password });
  const tokens = parseTokenPair(upstream.body);
  if (!tokens || upstream.status !== 200) {
    const problem = problemFromUpstream(upstream, "Sign in failed");
    return NextResponse.json(problem, { status: problem.status || 401 });
  }

  const response = NextResponse.json(sessionInfoFromAccessToken(tokens.accessToken));
  applyAuthCookies(response, tokens);
  return response;
}
