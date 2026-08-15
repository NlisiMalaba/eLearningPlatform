import type { ContentType } from "@/lib/content/types";

export const UPLOADABLE_TYPES = ["Pdf", "Video", "Audio"] as const;

export type UploadableType = (typeof UPLOADABLE_TYPES)[number];

export const MAX_VIDEO_BYTES = 500 * 1024 * 1024;
export const MAX_AUDIO_BYTES = 50 * 1024 * 1024;
export const MAX_OTHER_BYTES = 100 * 1024 * 1024;

export type UploadValidationError = "type" | "size" | "empty";

export function maxBytesForContentType(type: ContentType): number {
  switch (type) {
    case "Video":
    case "Animation":
      return MAX_VIDEO_BYTES;
    case "Audio":
      return MAX_AUDIO_BYTES;
    default:
      return MAX_OTHER_BYTES;
  }
}

export function inferUploadType(file: File): UploadableType | null {
  const name = file.name.toLowerCase();
  const mime = file.type.toLowerCase();
  if (mime === "application/pdf" || name.endsWith(".pdf")) {
    return "Pdf";
  }

  if (mime === "video/mp4" || name.endsWith(".mp4")) {
    return "Video";
  }

  if (
    mime === "audio/mpeg" ||
    mime === "audio/mp3" ||
    mime === "audio/wav" ||
    mime === "audio/x-wav" ||
    mime === "audio/wave" ||
    name.endsWith(".mp3") ||
    name.endsWith(".wav")
  ) {
    return "Audio";
  }

  return null;
}

export function validateUploadFile(
  file: File,
  type: UploadableType,
): UploadValidationError | null {
  if (file.size <= 0) {
    return "empty";
  }

  const inferred = inferUploadType(file);
  if (inferred !== type) {
    return "type";
  }

  if (file.size > maxBytesForContentType(type)) {
    return "size";
  }

  return null;
}
