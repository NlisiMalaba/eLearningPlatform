import * as Speech from "expo-speech";
import { configureSpeechEngine, type SpeechEngine } from "@/lib/accessibility/speech";

const expoSpeechEngine: SpeechEngine = {
  speak(text, languageTag, handlers) {
    Speech.speak(text, {
      language: languageTag,
      onDone: handlers.onEnd,
      onStopped: handlers.onEnd,
      onError: handlers.onError,
    });
  },
  stop() {
    return Speech.stop();
  },
};

export function installExpoSpeechEngine(): void {
  configureSpeechEngine(expoSpeechEngine);
}
