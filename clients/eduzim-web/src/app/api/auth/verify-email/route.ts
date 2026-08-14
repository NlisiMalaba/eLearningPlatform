import { NextRequest, NextResponse } from "next/server";
import { postUpstreamJson, problemFromUpstream } from "@/lib/auth/upstreamAuth";

export async function POST(request: NextRequest): Promise<NextResponse> {
  const payload = (await request.json().catch(() => null)) as {
    userId?: unknown;
    token?: unknown;
  } | null;

  const userId = typeof payload?.userId === "string" ? payload.userId : "";
  const token = typeof payload?.token === "string" ? payload.token : "";
  if (!userId || !token) {
    return NextResponse.json(
      {
        type: "https://eduzim.co.zw/errors/validation",
        title: "Validation failed",
        status: 400,
        detail: "Verification link is missing user or token.",
      },
      { status: 400 },
    );
  }

  const upstream = await postUpstreamJson("/api/v1/auth/verify-email", { userId, token });
  if (upstream.status < 200 || upstream.status >= 300) {
    const problem = problemFromUpstream(upstream, "Email verification failed");
    return NextResponse.json(problem, { status: problem.status || 400 });
  }

  return NextResponse.json({ verified: true });
}
