const LOCAL_JOIN_PREFIX = "local-join:";

export type VideoProvider = "daily" | "jitsi";

export type VideoEmbed = {
  provider: VideoProvider;
  src: string;
};

export function resolveVideoEmbed(
  roomId: string,
  token: string,
  dailyDomain = process.env.NEXT_PUBLIC_DAILY_DOMAIN,
): VideoEmbed {
  const host = dailyHost(dailyDomain);
  if (host && !token.startsWith(LOCAL_JOIN_PREFIX)) {
    const room = encodeURIComponent(roomId);
    const meetingToken = encodeURIComponent(token);
    return {
      provider: "daily",
      src: `https://${host}/${room}?t=${meetingToken}`,
    };
  }

  return {
    provider: "jitsi",
    src: `https://meet.jit.si/${encodeURIComponent(roomId)}`,
  };
}

function dailyHost(dailyDomain: string | undefined): string | null {
  const domain = dailyDomain?.trim().replace(/^https?:\/\//, "").replace(/\/$/, "");
  if (!domain) {
    return null;
  }

  return domain.includes(".") ? domain : `${domain}.daily.co`;
}
