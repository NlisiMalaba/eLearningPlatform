import { rotateOrbit, zoomOrbit, type OrbitState } from "@/lib/content/sceneControls";

const TAP_MOVE_THRESHOLD = 8;

export type SceneGestureMode = "idle" | "pan" | "pinch";

export type SceneGestureSession = {
  mode: SceneGestureMode;
  startX: number;
  startY: number;
  lastX: number;
  lastY: number;
  lastPinchDistance: number;
  moved: boolean;
};

export function beginSceneGesture(
  x: number,
  y: number,
  touchCount: number,
  pinchDistance: number,
): SceneGestureSession {
  return {
    mode: touchCount >= 2 ? "pinch" : "pan",
    startX: x,
    startY: y,
    lastX: x,
    lastY: y,
    lastPinchDistance: pinchDistance,
    moved: false,
  };
}

export function applySceneGestureMove(
  session: SceneGestureSession,
  orbit: OrbitState,
  x: number,
  y: number,
  touchCount: number,
  pinchDistance: number,
  viewWidth: number,
): { session: SceneGestureSession; orbit: OrbitState } {
  if (touchCount >= 2) {
    const nextSession: SceneGestureSession = {
      ...session,
      mode: "pinch",
      lastPinchDistance: pinchDistance,
      moved: true,
    };
    const previous = session.lastPinchDistance || pinchDistance;
    const delta = (previous - pinchDistance) * 0.05;
    return { session: nextSession, orbit: zoomOrbit(orbit, delta) };
  }

  const dx = x - session.lastX;
  const dy = y - session.lastY;
  const total = Math.hypot(x - session.startX, y - session.startY);
  const scale = viewWidth > 0 ? 3.2 / viewWidth : 0.01;
  return {
    session: {
      ...session,
      mode: "pan",
      lastX: x,
      lastY: y,
      moved: session.moved || total > TAP_MOVE_THRESHOLD,
    },
    orbit: rotateOrbit(orbit, dx * scale, dy * scale),
  };
}

export function isSceneTap(session: SceneGestureSession): boolean {
  return session.mode !== "pinch" && !session.moved;
}
