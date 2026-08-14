import type { ContentType, RendererKind } from "@/lib/content/types";

export function selectRendererKind(type: ContentType): RendererKind {
  switch (type) {
    case "Video":
    case "Animation":
      return "video";
    case "Pdf":
      return "pdf";
    case "Audio":
      return "audio";
    case "Scene3D":
      return "scene3d";
    case "Quiz":
      return "quiz";
    default:
      return "unsupported";
  }
}
