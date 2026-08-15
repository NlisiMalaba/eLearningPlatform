import * as THREE from "three";
import {
  applyKeyboardOrbit,
  createOrbitState,
  orbitToCartesian,
  rotateOrbit,
  zoomOrbit,
  type OrbitState,
} from "@/lib/content/sceneControls";
import { disposeObject, loadModelOrFallback, pickMesh } from "@/lib/content/scene3dHelpers";

export type Scene3DHandle = {
  reset: () => void;
  dispose: () => void;
};

export async function mountInteractiveScene(
  canvas: HTMLCanvasElement,
  modelUrl: string | undefined,
): Promise<Scene3DHandle> {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0xf7f5f0);
  const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 100);
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  addLights(scene);

  const root = new THREE.Group();
  scene.add(root);
  await loadModelOrFallback(root, modelUrl);

  let orbit = createOrbitState();
  applyCamera(camera, orbit);

  const bindings = bindInteraction(canvas, camera, renderer, root, () => orbit, (next) => {
    orbit = next;
    applyCamera(camera, orbit);
  });

  let frame = 0;
  const renderLoop = () => {
    frame = window.requestAnimationFrame(renderLoop);
    renderer.render(scene, camera);
  };
  renderLoop();

  return {
    reset: () => {
      orbit = createOrbitState();
      applyCamera(camera, orbit);
    },
    dispose: () => {
      window.cancelAnimationFrame(frame);
      bindings.dispose();
      renderer.dispose();
      disposeObject(root);
    },
  };
}

function addLights(scene: THREE.Scene): void {
  scene.add(new THREE.AmbientLight(0xffffff, 0.7));
  const keyLight = new THREE.DirectionalLight(0xffffff, 1.1);
  keyLight.position.set(4, 8, 6);
  scene.add(keyLight);
}

function applyCamera(camera: THREE.PerspectiveCamera, orbit: OrbitState): void {
  const { x, y, z } = orbitToCartesian(orbit);
  camera.position.set(x, y, z);
  camera.lookAt(0, 0, 0);
}

function bindInteraction(
  canvas: HTMLCanvasElement,
  camera: THREE.PerspectiveCamera,
  renderer: THREE.WebGLRenderer,
  root: THREE.Group,
  getOrbit: () => OrbitState,
  setOrbit: (orbit: OrbitState) => void,
): { dispose: () => void } {
  const pointer = { dragging: false, moved: false, lastX: 0, lastY: 0 };
  const raycaster = new THREE.Raycaster();
  const pointerNdc = new THREE.Vector2();
  let selected: THREE.Mesh | null = null;

  const resize = () => {
    const width = canvas.clientWidth || 640;
    const height = canvas.clientHeight || 360;
    camera.aspect = width / height;
    camera.updateProjectionMatrix();
    renderer.setSize(width, height, false);
  };
  resize();

  const onPointerDown = (event: PointerEvent) => {
    pointer.dragging = true;
    pointer.moved = false;
    pointer.lastX = event.clientX;
    pointer.lastY = event.clientY;
    canvas.setPointerCapture(event.pointerId);
  };

  const onPointerMove = (event: PointerEvent) => {
    if (!pointer.dragging) {
      return;
    }

    const dx = event.clientX - pointer.lastX;
    const dy = event.clientY - pointer.lastY;
    if (Math.abs(dx) + Math.abs(dy) > 2) {
      pointer.moved = true;
    }

    pointer.lastX = event.clientX;
    pointer.lastY = event.clientY;
    setOrbit(rotateOrbit(getOrbit(), dx * 0.01, dy * 0.01));
  };

  const onPointerUp = (event: PointerEvent) => {
    pointer.dragging = false;
    if (!pointer.moved) {
      selected = pickMesh(canvas, camera, root, raycaster, pointerNdc, event, selected);
    }
  };

  const onWheel = (event: WheelEvent) => {
    event.preventDefault();
    setOrbit(zoomOrbit(getOrbit(), event.deltaY * 0.01));
  };

  const onKeyDown = (event: KeyboardEvent) => {
    const next = applyKeyboardOrbit(getOrbit(), event.key);
    if (!next) {
      return;
    }

    event.preventDefault();
    setOrbit(next);
  };

  canvas.addEventListener("pointerdown", onPointerDown);
  canvas.addEventListener("pointermove", onPointerMove);
  canvas.addEventListener("pointerup", onPointerUp);
  canvas.addEventListener("wheel", onWheel, { passive: false });
  canvas.addEventListener("keydown", onKeyDown);
  window.addEventListener("resize", resize);

  return {
    dispose: () => {
      window.removeEventListener("resize", resize);
      canvas.removeEventListener("pointerdown", onPointerDown);
      canvas.removeEventListener("pointermove", onPointerMove);
      canvas.removeEventListener("pointerup", onPointerUp);
      canvas.removeEventListener("wheel", onWheel);
      canvas.removeEventListener("keydown", onKeyDown);
    },
  };
}
