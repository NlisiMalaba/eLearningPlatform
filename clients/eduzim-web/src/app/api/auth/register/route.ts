import { NextRequest, NextResponse } from "next/server";
import { PUBLIC_REGISTER_ROLES, type PublicRegisterRole } from "@/lib/auth/types";
import { postUpstreamJson, problemFromUpstream } from "@/lib/auth/upstreamAuth";

export async function POST(request: NextRequest): Promise<NextResponse> {
  const payload = (await request.json().catch(() => null)) as Record<string, unknown> | null;
  const email = typeof payload?.email === "string" ? payload.email.trim() : "";
  const password = typeof payload?.password === "string" ? payload.password : "";
  const fullName = typeof payload?.fullName === "string" ? payload.fullName.trim() : "";
  const role = payload?.role;

  if (!email || !password || !fullName || !isPublicRole(role)) {
    return NextResponse.json(
      {
        type: "https://eduzim.co.zw/errors/validation",
        title: "Validation failed",
        status: 400,
        detail: "Name, email, password, and a valid role are required.",
      },
      { status: 400 },
    );
  }

  const upstream = await postUpstreamJson("/api/v1/auth/register", {
    email,
    password,
    fullName,
    role,
  });

  if (upstream.status !== 201 && upstream.status !== 200) {
    const problem = problemFromUpstream(upstream, "Registration failed");
    return NextResponse.json(problem, { status: problem.status || 400 });
  }

  const userId = readUserId(upstream.body);
  return NextResponse.json({ userId }, { status: 201 });
}

function isPublicRole(value: unknown): value is PublicRegisterRole {
  return PUBLIC_REGISTER_ROLES.includes(value as PublicRegisterRole);
}

function readUserId(body: unknown): string | null {
  if (!body || typeof body !== "object" || !("userId" in body)) {
    return null;
  }

  const userId = body.userId;
  return typeof userId === "string" ? userId : null;
}
