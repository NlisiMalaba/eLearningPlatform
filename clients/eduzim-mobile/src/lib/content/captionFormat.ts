export function inferCaptionFormat(url: string): "vtt" | "srt" | "ttml" {
  const path = url.split("?")[0]?.toLowerCase() ?? "";
  if (path.endsWith(".srt")) {
    return "srt";
  }

  if (path.endsWith(".ttml") || path.endsWith(".xml")) {
    return "ttml";
  }

  return "vtt";
}
