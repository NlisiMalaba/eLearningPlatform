"use client";

import { usePathname } from "next/navigation";
import { type FormEvent, useId, useRef } from "react";
import { useFocusTrap } from "@/hooks/useFocusTrap";
import { t } from "@/lib/i18n/t";
import { isAssessmentPath, readModuleIdFromPath } from "@/lib/zimbot/learningContext";
import { ZIMBOT_LANGUAGES } from "@/lib/zimbot/languages";
import { useZimBotStore } from "@/lib/zimbot/store";
import { sendZimBotChat } from "@/lib/zimbot/zimbotService";
import { isZimBotUnavailable, ZIMBOT_UNAVAILABLE_REPLY } from "@/lib/zimbot/unavailable";
import type { ChatMessage } from "@/lib/zimbot/types";

type ZimBotPanelProps = {
  studentId: string;
  onClose: () => void;
};

export function ZimBotPanel({ studentId, onClose }: ZimBotPanelProps) {
  const pathname = usePathname();
  const titleId = useId();
  const panelRef = useRef<HTMLDivElement>(null);
  const language = useZimBotStore((state) => state.language);
  const messages = useZimBotStore((state) => state.messages);
  const sending = useZimBotStore((state) => state.sending);
  const setLanguage = useZimBotStore((state) => state.setLanguage);
  const appendMessage = useZimBotStore((state) => state.appendMessage);
  const setSending = useZimBotStore((state) => state.setSending);
  useFocusTrap(true, panelRef, onClose);

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const form = event.currentTarget;
    const field = form.elements.namedItem("message");
    if (!(field instanceof HTMLTextAreaElement)) {
      return;
    }

    const text = field.value.trim();
    if (!text || sending) {
      return;
    }

    field.value = "";
    appendMessage(studentMessage(text));
    setSending(true);
    try {
      const result = await sendZimBotChat({
        studentId,
        message: text,
        language,
        moduleId: readModuleIdFromPath(pathname),
        inAssessment: isAssessmentPath(pathname),
      });
      appendMessage(botMessage(result.reply, result.usedFallback, result.isLowConfidence));
    } catch (error: unknown) {
      if (isZimBotUnavailable(error, false)) {
        appendMessage(botMessage(ZIMBOT_UNAVAILABLE_REPLY, true, false));
      } else {
        appendMessage(botMessage(t("zimbot.error"), false, false));
      }
    } finally {
      setSending(false);
    }
  }

  const showFallback = messages.some((message) => message.usedFallback);

  return (
    <div
      ref={panelRef}
      id="zimbot-panel"
      role="dialog"
      aria-modal="true"
      aria-labelledby={titleId}
      className="flex h-[min(32rem,80vh)] w-[min(24rem,calc(100vw-2rem))] flex-col rounded-2xl bg-white shadow-lg ring-1 ring-black/10"
    >
      <header className="flex items-center justify-between gap-2 border-b border-zinc-200 px-4 py-3">
        <h2 id={titleId} className="text-base font-semibold text-[#0B6E4F]">
          {t("zimbot.title")}
        </h2>
        <button
          type="button"
          onClick={onClose}
          className="rounded-md px-2 py-1 text-sm font-medium"
        >
          {t("zimbot.close")}
        </button>
      </header>
      <label className="flex items-center gap-2 border-b border-zinc-200 px-4 py-2 text-sm">
        <span>{t("zimbot.language")}</span>
        <select
          className="flex-1 rounded-md border border-zinc-300 bg-white px-2 py-1"
          value={language}
          onChange={(event) => setLanguage(parseSelectedLanguage(event.target.value))}
          aria-label={t("zimbot.language")}
        >
          {ZIMBOT_LANGUAGES.map((item) => (
            <option key={item} value={item}>
              {t(languageKey(item))}
            </option>
          ))}
        </select>
      </label>
      <ul className="flex-1 space-y-3 overflow-y-auto px-4 py-3" aria-live="polite">
        {messages.length === 0 ? (
          <li className="text-sm text-zinc-600">{t("zimbot.empty")}</li>
        ) : (
          messages.map((message) => <ChatBubble key={message.id} message={message} />)
        )}
        {sending ? <li className="text-sm text-zinc-500">{t("zimbot.thinking")}</li> : null}
      </ul>
      {showFallback ? (
        <p className="border-t border-amber-200 bg-amber-50 px-4 py-2 text-sm" role="status">
          {t("zimbot.unavailable")}{" "}
          <a href="/help" className="font-medium text-[#0B6E4F] underline">
            {t("zimbot.helpLink")}
          </a>
        </p>
      ) : null}
      <form onSubmit={onSubmit} className="border-t border-zinc-200 p-3">
        <label className="sr-only" htmlFor="zimbot-message">
          {t("zimbot.input")}
        </label>
        <textarea
          id="zimbot-message"
          name="message"
          rows={2}
          required
          maxLength={4000}
          className="w-full rounded-lg border border-zinc-300 px-3 py-2 text-sm"
          placeholder={t("zimbot.placeholder")}
        />
        <button
          type="submit"
          disabled={sending}
          className="mt-2 rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
        >
          {t("zimbot.send")}
        </button>
      </form>
    </div>
  );
}

function ChatBubble({ message }: { message: ChatMessage }) {
  const isStudent = message.role === "student";
  return (
    <li className={isStudent ? "text-right" : "text-left"}>
      <p
        className={`inline-block max-w-[90%] rounded-2xl px-3 py-2 text-sm ${
          isStudent ? "bg-[#0B6E4F] text-white" : "bg-zinc-100 text-zinc-900"
        }`}
      >
        {message.text}
      </p>
    </li>
  );
}

function studentMessage(text: string): ChatMessage {
  return {
    id: crypto.randomUUID(),
    role: "student",
    text,
    usedFallback: false,
    isLowConfidence: false,
  };
}

function botMessage(text: string, usedFallback: boolean, isLowConfidence: boolean): ChatMessage {
  return {
    id: crypto.randomUUID(),
    role: "zimbot",
    text,
    usedFallback,
    isLowConfidence,
  };
}

function parseSelectedLanguage(value: string) {
  return ZIMBOT_LANGUAGES.find((item) => item === value) ?? "English";
}

function languageKey(language: (typeof ZIMBOT_LANGUAGES)[number]) {
  switch (language) {
    case "English":
      return "zimbot.lang.english" as const;
    case "Shona":
      return "zimbot.lang.shona" as const;
    case "Ndebele":
      return "zimbot.lang.ndebele" as const;
    case "Kalanga":
      return "zimbot.lang.kalanga" as const;
  }
}
