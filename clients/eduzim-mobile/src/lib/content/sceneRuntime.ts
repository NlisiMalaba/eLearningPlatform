import type { ExpoWebGLRenderingContext } from "expo-gl";
import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import {
  createOrbitState,
  isGltfUrl,
  orbitToCartesian,
  type OrbitState,
} from "@/lib/content/sceneControls";

export type MobileSceneHandle = {
  setOrbit: (orbit: OrbitState) => void;
  getOrbit: () => OrbitState;
  pickAtNdc: (x: number, y: number) => void;
  reset: () => void;
  dispose: () => void;
  resize: (width: number, height: number) => void;
};

type GlCanvas = {
  width: number;
  height: number;
  style: Record<string, never>;
  clientWidth: number;
  clientHeight: number;
  addEventListener: () => void;
  removeEventListener: () => void;
  getContext: () => ExpoWebGLRenderingContext;
};

export async function createMobileScene(
  gl: ExpoWebGLRenderingContext,
  modelUrl: string | undefined,
): Promise<MobileSceneHandle> {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0xf7f5f0);
  const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 100);
  const canvas = createGlCanvas(gl);
  const renderer = new THREE.WebGLRenderer({
    canvas: canvas as unknown as HTMLCanvasElement,
    context: gl as unknown as WebGLRenderingContext,
    antialias: true,
  });
  renderer.setSize(gl.drawingBufferWidth, gl.drawingBufferHeight);
  addLights(scene);

  const root = new THREE.Group();
  scene.add(root);
  await loadModelOrFallback(root, modelUrl);

  let orbit = createOrbitState();
  applyCamera(camera, orbit);

  const raycaster = new THREE.Raycaster();
  const pointerNdc = new THREE.Vector2();
  let selected: THREE.Mesh | null = null;
  let frame = 0;
  let disposed = false;

  const renderLoop = () => {
    if (disposed) {
      return;
    }

    frame = requestAnimationFrame(renderLoop);
    renderer.render(scene, camera);
    gl.endFrameEXP();
  };
  renderLoop();

  return {
    getOrbit: () => orbit,
    setOrbit: (next) => {
      orbit = next;
      applyCamera(camera, orbit);
    },
    pickAtNdc: (x, y) => {
      selected = pickMesh(camera, root, raycaster, pointerNdc, x, y, selected);
    },
    reset: () => {
      orbit = createOrbitState();
      applyCamera(camera, orbit);
      clearEmissive(selected);
      selected = null;
    },
    resize: (width, height) => {
      if (width <= 0 || height <= 0) {
        return;
      }

      camera.aspect = width / height;
      camera.updateProjectionMatrix();
    },
    dispose: () => {
      disposed = true;
      cancelAnimationFrame(frame);
      renderer.dispose();
      disposeObject(root);
    },
  };
}

function createGlCanvas(gl: ExpoWebGLRenderingContext): GlCanvas {
  return {
    width: gl.drawingBufferWidth,
    height: gl.drawingBufferHeight,
    style: {},
    clientWidth: gl.drawingBufferWidth,
    clientHeight: gl.drawingBufferHeight,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    getContext: () => gl,
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

async function loadModelOrFallback(root: THREE.Group, modelUrl: string | undefined): Promise<void> {
  if (modelUrl && isGltfUrl(modelUrl)) {
    try {
      const response = await fetch(modelUrl);
      if (response.ok) {
        const buffer = await response.arrayBuffer();
        const gltf = await parseGltf(buffer);
        root.add(gltf.scene);
        return;
      }
    } catch {
      // Fall through to the interactive default scene.
    }
  }

  const box = new THREE.Mesh(
    new THREE.BoxGeometry(1.6, 1.6, 1.6),
    new THREE.MeshStandardMaterial({ color: 0x0b6e4f }),
  );
  box.position.x = -1.2;
  const sphere = new THREE.Mesh(
    new THREE.SphereGeometry(0.9, 32, 32),
    new THREE.MeshStandardMaterial({ color: 0xd97706 }),
  );
  sphere.position.x = 1.4;
  root.add(box, sphere);
}

function parseGltf(buffer: ArrayBuffer): Promise<{ scene: THREE.Group }> {
  const loader = new GLTFLoader();
  return new Promise((resolve, reject) => {
    loader.parse(
      buffer,
      "",
      (gltf) => resolve({ scene: gltf.scene }),
      (error) => reject(error),
    );
  });
}

function pickMesh(
  camera: THREE.PerspectiveCamera,
  root: THREE.Group,
  raycaster: THREE.Raycaster,
  pointerNdc: THREE.Vector2,
  ndcX: number,
  ndcY: number,
  current: THREE.Mesh | null,
): THREE.Mesh | null {
  pointerNdc.set(ndcX, ndcY);
  raycaster.setFromCamera(pointerNdc, camera);
  const hit = raycaster.intersectObject(root, true)[0]?.object;
  if (!(hit instanceof THREE.Mesh)) {
    return current;
  }

  clearEmissive(current);
  if (hit === current) {
    return null;
  }

  if (hit.material instanceof THREE.MeshStandardMaterial) {
    hit.material.emissive.setHex(0x444444);
  }

  return hit;
}

function disposeObject(root: THREE.Object3D): void {
  root.traverse((child) => {
    if (!(child instanceof THREE.Mesh)) {
      return;
    }

    child.geometry.dispose();
    const material = child.material;
    if (Array.isArray(material)) {
      material.forEach((item) => item.dispose());
      return;
    }

    material.dispose();
  });
}

function clearEmissive(mesh: THREE.Mesh | null): void {
  if (mesh && mesh.material instanceof THREE.MeshStandardMaterial) {
    mesh.material.emissive.setHex(0x000000);
  }
}
