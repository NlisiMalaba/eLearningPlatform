import { describe, expect, it } from "vitest";
import { inferCaptionFormat } from "@/lib/content/captionFormat";
import {
  createOrbitState,
  isGltfUrl,
  orbitToCartesian,
  pointerToNdc,
  rotateOrbit,
  zoomOrbit,
} from "@/lib/content/sceneControls";
import {
  applySceneGestureMove,
  beginSceneGesture,
  isSceneTap,
} from "@/lib/content/sceneGestures";

describe("scene orbit controls", () => {
  it("rotates yaw and pitch and zooms within limits", () => {
    const start = createOrbitState();
    const rotated = rotateOrbit(start, 0.5, 0.2);
    expect(rotated.yaw).toBeGreaterThan(start.yaw);
    expect(rotated.pitch).toBeGreaterThan(start.pitch);

    const zoomedIn = zoomOrbit(start, -20);
    const zoomedOut = zoomOrbit(start, 20);
    expect(zoomedIn.distance).toBeLessThan(start.distance);
    expect(zoomedOut.distance).toBeGreaterThan(start.distance);
    expect(zoomedIn.distance).toBeGreaterThanOrEqual(2);
    expect(zoomedOut.distance).toBeLessThanOrEqual(24);
  });

  it("maps orbit state to a camera position looking at the origin", () => {
    const position = orbitToCartesian(createOrbitState());
    expect(Number.isFinite(position.x)).toBe(true);
    expect(Number.isFinite(position.y)).toBe(true);
    expect(Number.isFinite(position.z)).toBe(true);
  });

  it("detects glTF model URLs used by the 3D viewer", () => {
    expect(isGltfUrl("https://cdn.example/model.glb?token=1")).toBe(true);
    expect(isGltfUrl("https://cdn.example/scene.gltf")).toBe(true);
    expect(isGltfUrl("https://cdn.example/notes.pdf")).toBe(false);
  });

  it("converts a touch point into NDC for object picking", () => {
    expect(pointerToNdc(50, 25, 100, 50)).toEqual({ x: 0, y: 0 });
  });
});

describe("touch orbit gestures", () => {
  it("rotates on drag and treats a short press as a tap", () => {
    const start = createOrbitState();
    const session = beginSceneGesture(10, 10, 1, 0);
    expect(isSceneTap(session)).toBe(true);

    const moved = applySceneGestureMove(session, start, 40, 18, 1, 0, 320);
    expect(moved.orbit.yaw).not.toBe(start.yaw);
    expect(isSceneTap(moved.session)).toBe(false);
  });

  it("zooms when two fingers pinch", () => {
    const start = createOrbitState();
    const session = beginSceneGesture(10, 10, 2, 80);
    const pinched = applySceneGestureMove(session, start, 10, 10, 2, 40, 320);
    expect(pinched.orbit.distance).toBeGreaterThan(start.distance);
    expect(isSceneTap(pinched.session)).toBe(false);
  });
});

describe("caption formats", () => {
  it("infers VTT, SRT, and TTML from caption URLs", () => {
    expect(inferCaptionFormat("https://cdn.example/en.vtt?x=1")).toBe("vtt");
    expect(inferCaptionFormat("https://cdn.example/en.srt")).toBe("srt");
    expect(inferCaptionFormat("https://cdn.example/en.ttml")).toBe("ttml");
  });
});
