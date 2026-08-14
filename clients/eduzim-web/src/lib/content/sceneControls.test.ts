import { describe, expect, it } from "vitest";
import {
  applyKeyboardOrbit,
  createOrbitState,
  isGltfUrl,
  orbitToCartesian,
  rotateOrbit,
  zoomOrbit,
} from "@/lib/content/sceneControls";

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

  it("rotates and zooms from keyboard keys", () => {
    const start = createOrbitState();
    expect(applyKeyboardOrbit(start, "ArrowLeft")?.yaw).toBeLessThan(start.yaw);
    expect(applyKeyboardOrbit(start, "ArrowRight")?.yaw).toBeGreaterThan(start.yaw);
    expect(applyKeyboardOrbit(start, "+")?.distance).toBeLessThan(start.distance);
    expect(applyKeyboardOrbit(start, "-")?.distance).toBeGreaterThan(start.distance);
    expect(applyKeyboardOrbit(start, "a")).toBeNull();
  });

  it("detects glTF model URLs used by the 3D viewer", () => {
    expect(isGltfUrl("https://cdn.example/model.glb?token=1")).toBe(true);
    expect(isGltfUrl("https://cdn.example/scene.gltf")).toBe(true);
    expect(isGltfUrl("https://cdn.example/notes.pdf")).toBe(false);
  });
});
