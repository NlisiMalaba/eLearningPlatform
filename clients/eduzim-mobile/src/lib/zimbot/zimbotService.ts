import { apiFetch } from "@/lib/api/client";
import { parseZimBotLanguage } from "@/lib/zimbot/languages";
import type { SendZimBotChatInput, ZimBotChatResponse } from "@/lib/zimbot/types";

const CHAT_TIMEOUT_MS = 8_000;

export async function sendZimBotChat(input: SendZimBotChatInput): Promise<ZimBotChatResponse> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), CHAT_TIMEOUT_MS);
  try {
    const raw = await apiFetch<Record<string, unknown>>("/api/v1/zimbot/chat", {
      method: "POST",
      body: JSON.stringify({
        studentId: input.studentId,
        message: input.message,
        moduleId: input.moduleId,
        inAssessment: input.inAssessment,
        language: input.language,
      }),
      signal: controller.signal,
    });
    return mapChat(raw);
  } finally {
    clearTimeout(timer);
  }
}

function mapChat(raw: Record<string, unknown>): ZimBotChatResponse {
  return {
    interactionId: readString(raw.interactionId) ?? cryptoRandomId(),
    reply: readString(raw.reply) ?? "",
    language: parseZimBotLanguage(raw.language),
    usedHintMode: raw.usedHintMode === true,
    usedFallback: raw.usedFallback === true,
    isLowConfidence: raw.isLowConfidence === true,
  };
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

function cryptoRandomId(): string {
  const cryptoRef = globalThis.crypto;
  if (cryptoRef && typeof cryptoRef.randomUUID === "function") {
    return cryptoRef.randomUUID();
  }

  return "zimbot-interaction";
}
