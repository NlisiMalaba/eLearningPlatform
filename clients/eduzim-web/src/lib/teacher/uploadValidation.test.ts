import { describe, expect, it } from "vitest";
import {
  inferUploadType,
  MAX_AUDIO_BYTES,
  MAX_OTHER_BYTES,
  MAX_VIDEO_BYTES,
  validateUploadFile,
} from "@/lib/teacher/uploadValidation";

function file(name: string, size: number, type: string): File {
  return new File([new Uint8Array(Math.min(size, 8))], name, { type });
}

describe("uploadValidation", () => {
  it("infers pdf, mp4, and audio types from name and mime", () => {
    expect(inferUploadType(file("notes.pdf", 10, "application/pdf"))).toBe("Pdf");
    expect(inferUploadType(file("clip.mp4", 10, "video/mp4"))).toBe("Video");
    expect(inferUploadType(file("voice.mp3", 10, "audio/mpeg"))).toBe("Audio");
    expect(inferUploadType(file("voice.wav", 10, "audio/wav"))).toBe("Audio");
    expect(inferUploadType(file("model.glb", 10, "model/gltf-binary"))).toBeNull();
  });

  it("rejects oversized files before upload", () => {
    const video = file("clip.mp4", 8, "video/mp4");
    Object.defineProperty(video, "size", { value: MAX_VIDEO_BYTES + 1 });
    expect(validateUploadFile(video, "Video")).toBe("size");

    const audio = file("voice.mp3", 8, "audio/mpeg");
    Object.defineProperty(audio, "size", { value: MAX_AUDIO_BYTES + 1 });
    expect(validateUploadFile(audio, "Audio")).toBe("size");

    const pdf = file("notes.pdf", 8, "application/pdf");
    Object.defineProperty(pdf, "size", { value: MAX_OTHER_BYTES + 1 });
    expect(validateUploadFile(pdf, "Pdf")).toBe("size");
  });

  it("rejects type mismatches and empty files", () => {
    expect(validateUploadFile(file("notes.pdf", 10, "application/pdf"), "Video")).toBe("type");
    const empty = file("notes.pdf", 0, "application/pdf");
    Object.defineProperty(empty, "size", { value: 0 });
    expect(validateUploadFile(empty, "Pdf")).toBe("empty");
  });
});
