import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { isGltfUrl } from "@/lib/content/sceneControls";

export async function loadModelOrFallback(
  root: THREE.Group,
  modelUrl: string | undefined,
): Promise<void> {
  if (modelUrl && isGltfUrl(modelUrl)) {
    try {
      const gltf = await new GLTFLoader().loadAsync(modelUrl);
      root.add(gltf.scene);
      return;
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

export function pickMesh(
  canvas: HTMLCanvasElement,
  camera: THREE.PerspectiveCamera,
  root: THREE.Group,
  raycaster: THREE.Raycaster,
  pointerNdc: THREE.Vector2,
  event: PointerEvent,
  current: THREE.Mesh | null,
): THREE.Mesh | null {
  const rect = canvas.getBoundingClientRect();
  pointerNdc.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
  pointerNdc.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
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

export function disposeObject(root: THREE.Object3D): void {
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
