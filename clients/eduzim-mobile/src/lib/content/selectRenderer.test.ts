import { describe, expect, it } from "vitest";
import { parseContentType } from "@/lib/content/parseContentType";
import { selectRendererKind } from "@/lib/content/selectRenderer";
import { CONTENT_TYPES } from "@/lib/content/types";

describe("selectRendererKind", () => {
  it("maps video and animation to the captioned video player", () => {
    expect(selectRendererKind("Video")).toBe("video");
    expect(selectRendererKind("Animation")).toBe("video");
  });

  it("maps pdf, audio, 3d, and quiz to dedicated players", () => {
    expect(selectRendererKind("Pdf")).toBe("pdf");
    expect(selectRendererKind("Audio")).toBe("audio");
    expect(selectRendererKind("Scene3D")).toBe("scene3d");
    expect(selectRendererKind("Quiz")).toBe("quiz");
  });

  it("does not use a learning renderer for games", () => {
    expect(selectRendererKind("Game")).toBe("unsupported");
  });
});

describe("parseContentType", () => {
  it("accepts enum names and numeric API values", () => {
    CONTENT_TYPES.forEach((type, index) => {
      expect(parseContentType(type)).toBe(type);
      expect(parseContentType(index)).toBe(type);
    });
  });
});
