import { CONTENT_TYPES, type ContentType } from "@/lib/content/types";

const CONTENT_TYPE_BY_INDEX: Record<number, ContentType> = {
  0: "Video",
  1: "Pdf",
  2: "Audio",
  3: "Scene3D",
  4: "Animation",
  5: "Quiz",
  6: "Game",
};

export function parseContentType(value: unknown): ContentType {
  if (typeof value === "number" && Number.isInteger(value)) {
    const mapped = CONTENT_TYPE_BY_INDEX[value];
    if (mapped) {
      return mapped;
    }
  }

  if (typeof value === "string" && isContentType(value)) {
    return value;
  }

  return "Video";
}

export function isContentType(value: string): value is ContentType {
  return (CONTENT_TYPES as readonly string[]).includes(value);
}
