import { describe, expect, it } from "vitest";
import { cueTextAt, parseCaptionDocument } from "@/lib/content/parseCaptions";

describe("parseCaptionDocument", () => {
  it("reads WebVTT cues used for closed captions", () => {
    const cues = parseCaptionDocument(
      `WEBVTT

00:00:00.000 --> 00:00:01.500
Hello Harare

00:00:01.500 --> 00:00:03.000
Welcome to the lesson.`,
      "vtt",
    );

    expect(cues).toHaveLength(2);
    expect(cueTextAt(cues, 800)).toBe("Hello Harare");
    expect(cueTextAt(cues, 2000)).toBe("Welcome to the lesson.");
    expect(cueTextAt(cues, 4000)).toBeNull();
  });

  it("reads SRT timestamps with comma decimals", () => {
    const cues = parseCaptionDocument(
      `1
00:00:00,000 --> 00:00:02,000
First line`,
      "srt",
    );

    expect(cues[0]?.text).toBe("First line");
    expect(cueTextAt(cues, 1000)).toBe("First line");
  });
});
