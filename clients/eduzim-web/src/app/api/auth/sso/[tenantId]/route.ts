import { NextResponse } from "next/server";
import { getUpstream, problemFromUpstream } from "@/lib/auth/upstreamAuth";

type RouteContext = {
  params: Promise<{ tenantId: string }>;
};

export async function GET(_request: Request, context: RouteContext): Promise<NextResponse> {
  const { tenantId } = await context.params;
  if (!isGuid(tenantId)) {
    return NextResponse.json(
      {
        type: "https://eduzim.co.zw/errors/validation",
        title: "Validation failed",
        status: 400,
        detail: "A valid school identifier is required.",
      },
      { status: 400 },
    );
  }

  const upstream = await getUpstream(`/api/v1/auth/sso/${tenantId}`);
  if (upstream.location) {
    return NextResponse.json({ redirectUrl: upstream.location });
  }

  const problem = problemFromUpstream(upstream, "SSO is not configured for this school");
  return NextResponse.json(problem, { status: problem.status || 404 });
}

function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
    value,
  );
}
