export type CaptionCue = {
  startMs: number;
  endMs: number;
  text: string;
};

export function parseCaptionDocument(text: string, format: "vtt" | "srt" | "ttml"): CaptionCue[] {
  if (format === "ttml") {
    return parseTtml(text);
  }

  return parseTimedText(text);
}

export function cueTextAt(cues: readonly CaptionCue[], positionMs: number): string | null {
  const cue = cues.find((item) => positionMs >= item.startMs && positionMs <= item.endMs);
  return cue?.text ?? null;
}

function parseTimedText(text: string): CaptionCue[] {
  const blocks = text.replace(/\r\n/g, "\n").split(/\n\n+/);
  return blocks.flatMap((block) => {
    const lines = block.split("\n").filter((line) => line.trim().length > 0 && line.trim() !== "WEBVTT");
    const timing = lines.find((line) => line.includes("-->"));
    if (!timing) {
      return [];
    }

    const [startRaw, endRaw] = timing.split("-->").map((part) => part.trim().split(" ")[0]);
    const startMs = parseTimestamp(startRaw ?? "");
    const endMs = parseTimestamp(endRaw ?? "");
    const payload = lines
      .filter((line) => line !== timing && !/^\d+$/.test(line.trim()))
      .join("\n")
      .trim();
    if (startMs === null || endMs === null || payload.length === 0) {
      return [];
    }

    return [{ startMs, endMs, text: payload }];
  });
}

function parseTtml(text: string): CaptionCue[] {
  const matches = text.matchAll(/<p\b([^>]*)>([\s\S]*?)<\/p>/gi);
  const cues: CaptionCue[] = [];
  for (const match of matches) {
    const attrs = match[1] ?? "";
    const begin = /begin="([^"]+)"/i.exec(attrs)?.[1];
    const end = /end="([^"]+)"/i.exec(attrs)?.[1];
    const startMs = parseTimestamp(begin ?? "");
    const endMs = parseTimestamp(end ?? "");
    const payload = (match[2] ?? "").replace(/<[^>]+>/g, " ").replace(/\s+/g, " ").trim();
    if (startMs === null || endMs === null || payload.length === 0) {
      continue;
    }

    cues.push({ startMs, endMs, text: payload });
  }

  return cues;
}

function parseTimestamp(value: string): number | null {
  const trimmed = value.trim().replace(",", ".");
  const parts = trimmed.split(":");
  if (parts.length < 2 || parts.length > 3) {
    return null;
  }

  const secondsPart = parts[parts.length - 1];
  const minutesPart = parts[parts.length - 2];
  const hoursPart = parts.length === 3 ? parts[0] : "0";
  const hours = Number(hoursPart);
  const minutes = Number(minutesPart);
  const seconds = Number(secondsPart);
  if (![hours, minutes, seconds].every((item) => Number.isFinite(item))) {
    return null;
  }

  return Math.round((hours * 3600 + minutes * 60 + seconds) * 1000);
}
