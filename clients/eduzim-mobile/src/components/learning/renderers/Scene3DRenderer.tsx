import { GLView, type ExpoWebGLRenderingContext } from "expo-gl";
import { useEffect, useRef, useState, type Dispatch, type MutableRefObject, type SetStateAction } from "react";
import { PanResponder, Pressable, StyleSheet, Text, View } from "react-native";
import {
  applySceneGestureMove,
  beginSceneGesture,
  isSceneTap,
  type SceneGestureSession,
} from "@/lib/content/sceneGestures";
import { pointerToNdc, zoomOrbit } from "@/lib/content/sceneControls";
import { createMobileScene, type MobileSceneHandle } from "@/lib/content/sceneRuntime";
import { t } from "@/lib/i18n/t";
import { colors } from "@/theme";

type Scene3DRendererProps = {
  src: string;
  title: string;
};

export function Scene3DRenderer({ src, title }: Scene3DRendererProps) {
  const handleRef = useRef<MobileSceneHandle | null>(null);
  const gestureRef = useRef<SceneGestureSession | null>(null);
  const sizeRef = useRef({ width: 1, height: 1 });
  const [ready, setReady] = useState(false);

  useEffect(() => {
    setReady(false);
    return () => {
      handleRef.current?.dispose();
      handleRef.current = null;
    };
  }, [src]);

  const panResponder = useRef(
    PanResponder.create({
      onStartShouldSetPanResponder: () => true,
      onMoveShouldSetPanResponder: () => true,
      onPanResponderGrant: (event) => {
        const touch = event.nativeEvent;
        const pinch = pinchDistance(event.nativeEvent.touches);
        gestureRef.current = beginSceneGesture(
          touch.locationX,
          touch.locationY,
          event.nativeEvent.touches.length,
          pinch,
        );
      },
      onPanResponderMove: (event) => {
        const handle = handleRef.current;
        const session = gestureRef.current;
        if (!handle || !session) {
          return;
        }

        const touch = event.nativeEvent;
        const pinch = pinchDistance(event.nativeEvent.touches);
        const next = applySceneGestureMove(
          session,
          handle.getOrbit(),
          touch.locationX,
          touch.locationY,
          event.nativeEvent.touches.length,
          pinch,
          sizeRef.current.width,
        );
        gestureRef.current = next.session;
        handle.setOrbit(next.orbit);
      },
      onPanResponderRelease: (event) => {
        const handle = handleRef.current;
        const session = gestureRef.current;
        if (handle && session && isSceneTap(session)) {
          const ndc = pointerToNdc(
            event.nativeEvent.locationX,
            event.nativeEvent.locationY,
            sizeRef.current.width,
            sizeRef.current.height,
          );
          handle.pickAtNdc(ndc.x, ndc.y);
        }

        gestureRef.current = null;
      },
      onPanResponderTerminate: () => {
        gestureRef.current = null;
      },
    }),
  ).current;

  return (
    <View style={styles.stack}>
      <View
        style={styles.stage}
        onLayout={(event) => {
          const { width, height } = event.nativeEvent.layout;
          sizeRef.current = { width, height };
          handleRef.current?.resize(width, height);
        }}
        {...panResponder.panHandlers}
        accessibilityRole="image"
        accessibilityLabel={`${t("content.scene.label")}: ${title}. ${t("content.scene.hint")}`}
      >
        <GLView
          key={src}
          style={StyleSheet.absoluteFill}
          onContextCreate={(gl) => {
            void onContextCreate(gl, src, handleRef, setReady);
          }}
        />
        {!ready ? (
          <View style={styles.loading} pointerEvents="none">
            <Text style={styles.muted}>{t("content.loading")}</Text>
          </View>
        ) : null}
      </View>
      <Text style={styles.muted}>{t("content.scene.hint")}</Text>
      <View style={styles.controls}>
        <ControlButton
          label={t("content.scene.reset")}
          onPress={() => handleRef.current?.reset()}
        />
        <ControlButton
          label={t("content.scene.zoomIn")}
          onPress={() => {
            const handle = handleRef.current;
            if (handle) {
              handle.setOrbit(zoomOrbit(handle.getOrbit(), -0.8));
            }
          }}
        />
        <ControlButton
          label={t("content.scene.zoomOut")}
          onPress={() => {
            const handle = handleRef.current;
            if (handle) {
              handle.setOrbit(zoomOrbit(handle.getOrbit(), 0.8));
            }
          }}
        />
      </View>
    </View>
  );
}

function ControlButton({ label, onPress }: { label: string; onPress: () => void }) {
  return (
    <Pressable accessibilityRole="button" onPress={onPress} style={styles.control}>
      <Text style={styles.controlLabel}>{label}</Text>
    </Pressable>
  );
}

async function onContextCreate(
  gl: ExpoWebGLRenderingContext,
  src: string,
  handleRef: MutableRefObject<MobileSceneHandle | null>,
  setReady: Dispatch<SetStateAction<boolean>>,
): Promise<void> {
  handleRef.current?.dispose();
  const handle = await createMobileScene(gl, src);
  handleRef.current = handle;
  setReady(true);
}

function pinchDistance(touches: readonly { pageX: number; pageY: number }[]): number {
  if (touches.length < 2) {
    return 0;
  }

  return Math.hypot(touches[0].pageX - touches[1].pageX, touches[0].pageY - touches[1].pageY);
}

const styles = StyleSheet.create({
  stack: {
    gap: 12,
  },
  stage: {
    height: 320,
    borderRadius: 12,
    overflow: "hidden",
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: "#F7F5F0",
  },
  loading: {
    ...StyleSheet.absoluteFillObject,
    alignItems: "center",
    justifyContent: "center",
  },
  muted: {
    color: colors.muted,
    fontSize: 14,
  },
  controls: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
  },
  control: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    minHeight: 44,
    justifyContent: "center",
  },
  controlLabel: {
    fontSize: 14,
    fontWeight: "500",
    color: colors.text,
  },
});
