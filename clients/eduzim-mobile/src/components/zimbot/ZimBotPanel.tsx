import { useRef, useState } from "react";
import {
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import { createId } from "@/lib/ids";
import { t } from "@/lib/i18n/t";
import { useLearningContext } from "@/lib/learning/context";
import { ZIMBOT_LANGUAGES, type ZimBotLanguage } from "@/lib/zimbot/languages";
import {
  appendZimBotMessage,
  setZimBotHelpOpen,
  setZimBotLanguage,
  setZimBotSending,
  useZimBotStore,
} from "@/lib/zimbot/store";
import type { ChatMessage } from "@/lib/zimbot/types";
import { isZimBotUnavailable, ZIMBOT_UNAVAILABLE_REPLY } from "@/lib/zimbot/unavailable";
import { sendZimBotChat } from "@/lib/zimbot/zimbotService";
import { colors } from "@/theme";

type ZimBotPanelProps = {
  studentId: string;
  onClose: () => void;
};

export function ZimBotPanel({ studentId, onClose }: ZimBotPanelProps) {
  const { language, messages, sending, helpOpen } = useZimBotStore();
  const { moduleId, inAssessment } = useLearningContext();
  const [draft, setDraft] = useState("");
  const listRef = useRef<ScrollView>(null);
  const showFallback = messages.some((message) => message.usedFallback);

  async function onSend(): Promise<void> {
    const text = draft.trim();
    if (!text || sending) {
      return;
    }

    setDraft("");
    appendZimBotMessage(studentMessage(text));
    setZimBotSending(true);
    try {
      const result = await sendZimBotChat({
        studentId,
        message: text,
        language,
        moduleId,
        inAssessment,
      });
      appendZimBotMessage(botMessage(result.reply, result.usedFallback, result.isLowConfidence));
    } catch (error: unknown) {
      if (isZimBotUnavailable(error, false)) {
        appendZimBotMessage(botMessage(ZIMBOT_UNAVAILABLE_REPLY, true, false));
      } else {
        appendZimBotMessage(botMessage(t("zimbot.error"), false, false));
      }
    } finally {
      setZimBotSending(false);
    }
  }

  return (
    <View accessibilityLabel={t("zimbot.title")} style={styles.panel}>
      <View style={styles.header}>
        <Text style={styles.title}>{t("zimbot.title")}</Text>
        <Pressable accessibilityRole="button" onPress={onClose} style={styles.headerButton}>
          <Text style={styles.headerButtonLabel}>{t("zimbot.close")}</Text>
        </Pressable>
      </View>
      <View accessibilityLabel={t("zimbot.language")} style={styles.languages}>
        {ZIMBOT_LANGUAGES.map((item) => {
          const selected = item === language;
          return (
            <Pressable
              key={item}
              accessibilityRole="button"
              accessibilityState={{ selected }}
              onPress={() => setZimBotLanguage(item)}
              style={[styles.langChip, selected ? styles.langChipOn : null]}
            >
              <Text style={[styles.langChipLabel, selected ? styles.langChipLabelOn : null]}>
                {languageLabel(item)}
              </Text>
            </Pressable>
          );
        })}
      </View>
      <ScrollView
        ref={listRef}
        style={styles.messages}
        onContentSizeChange={() => listRef.current?.scrollToEnd({ animated: true })}
      >
        {messages.length === 0 ? (
          <Text style={styles.muted}>{t("zimbot.empty")}</Text>
        ) : (
          messages.map((message) => <ChatBubble key={message.id} message={message} />)
        )}
        {sending ? <Text style={styles.muted}>{t("zimbot.thinking")}</Text> : null}
      </ScrollView>
      {showFallback ? (
        <View style={styles.fallback}>
          <Text style={styles.fallbackText}>{t("zimbot.unavailable")}</Text>
          <Pressable accessibilityRole="button" onPress={() => setZimBotHelpOpen(true)}>
            <Text style={styles.link}>{t("zimbot.helpLink")}</Text>
          </Pressable>
        </View>
      ) : null}
      <View style={styles.composer}>
        <TextInput
          accessibilityLabel={t("zimbot.input")}
          placeholder={t("zimbot.placeholder")}
          value={draft}
          onChangeText={setDraft}
          multiline
          maxLength={4000}
          style={styles.input}
        />
        <Pressable
          accessibilityRole="button"
          disabled={sending}
          onPress={() => {
            void onSend();
          }}
          style={[styles.send, sending ? styles.sendDisabled : null]}
        >
          <Text style={styles.sendLabel}>{t("zimbot.send")}</Text>
        </Pressable>
      </View>
      <Modal visible={helpOpen} animationType="slide" onRequestClose={() => setZimBotHelpOpen(false)}>
        <View style={styles.help}>
          <Text style={styles.helpTitle}>{t("help.title")}</Text>
          <Text style={styles.muted}>{t("help.intro")}</Text>
          <Text style={styles.helpTip}>{t("help.tip.module")}</Text>
          <Text style={styles.helpTip}>{t("help.tip.teacher")}</Text>
          <Text style={styles.helpTip}>{t("help.tip.retry")}</Text>
          <Pressable
            accessibilityRole="button"
            onPress={() => setZimBotHelpOpen(false)}
            style={styles.send}
          >
            <Text style={styles.sendLabel}>{t("help.close")}</Text>
          </Pressable>
        </View>
      </Modal>
    </View>
  );
}

function ChatBubble({ message }: { message: ChatMessage }) {
  const isStudent = message.role === "student";
  return (
    <View style={[styles.bubbleRow, isStudent ? styles.bubbleRight : styles.bubbleLeft]}>
      <Text style={[styles.bubble, isStudent ? styles.bubbleStudent : styles.bubbleBot]}>
        {message.text}
      </Text>
    </View>
  );
}

function studentMessage(text: string): ChatMessage {
  return {
    id: createId(),
    role: "student",
    text,
    usedFallback: false,
    isLowConfidence: false,
  };
}

function botMessage(text: string, usedFallback: boolean, isLowConfidence: boolean): ChatMessage {
  return {
    id: createId(),
    role: "zimbot",
    text,
    usedFallback,
    isLowConfidence,
  };
}

function languageLabel(language: ZimBotLanguage): string {
  switch (language) {
    case "English":
      return t("zimbot.lang.english");
    case "Shona":
      return t("zimbot.lang.shona");
    case "Ndebele":
      return t("zimbot.lang.ndebele");
    case "Kalanga":
      return t("zimbot.lang.kalanga");
  }
}

const styles = StyleSheet.create({
  panel: {
    height: 480,
    maxHeight: "80%",
    width: "100%",
    backgroundColor: colors.surface,
    borderRadius: 16,
    overflow: "hidden",
    borderWidth: 1,
    borderColor: colors.border,
  },
  header: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  title: {
    fontSize: 16,
    fontWeight: "600",
    color: colors.brand,
  },
  headerButton: {
    minHeight: 44,
    justifyContent: "center",
    paddingHorizontal: 8,
  },
  headerButtonLabel: {
    fontSize: 14,
    fontWeight: "500",
    color: colors.text,
  },
  languages: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  langChip: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 999,
    paddingHorizontal: 12,
    paddingVertical: 8,
    minHeight: 44,
    justifyContent: "center",
  },
  langChipOn: {
    backgroundColor: colors.brand,
    borderColor: colors.brand,
  },
  langChipLabel: {
    fontSize: 13,
    color: colors.text,
  },
  langChipLabelOn: {
    color: colors.surface,
    fontWeight: "600",
  },
  messages: {
    flex: 1,
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  bubbleRow: {
    marginBottom: 10,
  },
  bubbleLeft: {
    alignItems: "flex-start",
  },
  bubbleRight: {
    alignItems: "flex-end",
  },
  bubble: {
    maxWidth: "90%",
    borderRadius: 16,
    paddingHorizontal: 12,
    paddingVertical: 8,
    overflow: "hidden",
    fontSize: 14,
  },
  bubbleStudent: {
    backgroundColor: colors.brand,
    color: colors.surface,
  },
  bubbleBot: {
    backgroundColor: "#F4F4F5",
    color: colors.text,
  },
  muted: {
    color: colors.muted,
    fontSize: 14,
  },
  fallback: {
    borderTopWidth: 1,
    borderTopColor: colors.warningBorder,
    backgroundColor: colors.warningBg,
    paddingHorizontal: 16,
    paddingVertical: 10,
    gap: 6,
  },
  fallbackText: {
    color: colors.warningText,
    fontSize: 14,
  },
  link: {
    color: colors.brand,
    fontWeight: "600",
    textDecorationLine: "underline",
  },
  composer: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    padding: 12,
    gap: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    minHeight: 56,
    fontSize: 14,
  },
  send: {
    alignSelf: "flex-start",
    backgroundColor: colors.brand,
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 10,
    minHeight: 44,
    justifyContent: "center",
  },
  sendDisabled: {
    opacity: 0.6,
  },
  sendLabel: {
    color: colors.surface,
    fontWeight: "600",
    fontSize: 14,
  },
  help: {
    flex: 1,
    padding: 24,
    gap: 12,
    backgroundColor: colors.background,
  },
  helpTitle: {
    fontSize: 24,
    fontWeight: "600",
    color: colors.text,
  },
  helpTip: {
    fontSize: 16,
    color: colors.text,
  },
});
