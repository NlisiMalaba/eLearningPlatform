export type OrbitState = {
  yaw: number;
  pitch: number;
  distance: number;
};

const MIN_DISTANCE = 2;
const MAX_DISTANCE = 24;
const MIN_PITCH = -Math.PI / 2 + 0.05;
const MAX_PITCH = Math.PI / 2 - 0.05;

export function createOrbitState(): OrbitState {
  return { yaw: 0.6, pitch: 0.35, distance: 8 };
}

export function rotateOrbit(state: OrbitState, deltaX: number, deltaY: number): OrbitState {
  const yaw = state.yaw + deltaX;
  const pitch = clamp(state.pitch + deltaY, MIN_PITCH, MAX_PITCH);
  return { ...state, yaw, pitch };
}

export function zoomOrbit(state: OrbitState, delta: number): OrbitState {
  return {
    ...state,
    distance: clamp(state.distance + delta, MIN_DISTANCE, MAX_DISTANCE),
  };
}

export function orbitToCartesian(state: OrbitState): { x: number; y: number; z: number } {
  const x = state.distance * Math.sin(state.yaw) * Math.cos(state.pitch);
  const y = state.distance * Math.sin(state.pitch);
  const z = state.distance * Math.cos(state.yaw) * Math.cos(state.pitch);
  return { x, y, z };
}

export function isGltfUrl(url: string): boolean {
  const path = url.split("?")[0]?.toLowerCase() ?? "";
  return path.endsWith(".gltf") || path.endsWith(".glb");
}

export function pointerToNdc(
  x: number,
  y: number,
  width: number,
  height: number,
): { x: number; y: number } {
  if (width <= 0 || height <= 0) {
    return { x: 0, y: 0 };
  }

  return {
    x: (x / width) * 2 - 1,
    y: -(y / height) * 2 + 1,
  };
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}
