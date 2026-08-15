import type { ZimBotLanguage } from "@/lib/zimbot/languages";

export type ChatRole = "student" | "zimbot";

export type ChatMessage = {
  id: string;
  role: ChatRole;
  text: string;
  usedFallback: boolean;
  isLowConfidence: boolean;
};

export type ZimBotChatResponse = {
  interactionId: string;
  reply: string;
  language: ZimBotLanguage | string;
  usedHintMode: boolean;
  usedFallback: boolean;
  isLowConfidence: boolean;
};

export type SendZimBotChatInput = {
  studentId: string;
  message: string;
  language: ZimBotLanguage;
  moduleId?: string;
  inAssessment: boolean;
};
