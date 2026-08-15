import { apiFetch } from "@/lib/api/client";
import { parseContentType } from "@/lib/content/parseContentType";
import type {
  CaptionTrack,
  ContentDetail,
  ModuleContentItem,
  ModuleDetail,
  ModuleListItem,
  TranscriptLink,
} from "@/lib/content/types";

export async function listModules(): Promise<ModuleListItem[]> {
  const raw = await apiFetch<unknown[]>("/api/v1/modules");
  if (!Array.isArray(raw)) {
    return [];
  }

  return raw.flatMap(mapModuleListItem);
}

export async function getModule(moduleId: string): Promise<ModuleDetail> {
  const raw = await apiFetch<Record<string, unknown>>(`/api/v1/modules/${moduleId}`);
  return mapModule(raw);
}

export async function getContent(contentId: string): Promise<ContentDetail> {
  const raw = await apiFetch<Record<string, unknown>>(`/api/v1/content/${contentId}`);
  return mapContent(raw);
}

export async function getCaptions(contentId: string): Promise<CaptionTrack[]> {
  const raw = await apiFetch<unknown[]>(`/api/v1/content/${contentId}/captions`);
  if (!Array.isArray(raw)) {
    return [];
  }

  return raw.flatMap(mapCaption);
}

export async function getTranscript(contentId: string): Promise<TranscriptLink | null> {
  try {
    const raw = await apiFetch<Record<string, unknown>>(`/api/v1/content/${contentId}/transcript`);
    const signedUrl = readString(raw.signedUrl);
    const transcriptId = readString(raw.transcriptId);
    if (!signedUrl || !transcriptId) {
      return null;
    }

    return { transcriptId, signedUrl };
  } catch {
    return null;
  }
}

export async function fetchQuizJson(downloadUrl: string): Promise<unknown> {
  const response = await fetch(downloadUrl);
  if (!response.ok) {
    throw new Error("Quiz payload could not be loaded.");
  }

  return response.json() as Promise<unknown>;
}

export async function fetchTranscriptText(signedUrl: string): Promise<string | null> {
  try {
    const response = await fetch(signedUrl);
    if (!response.ok) {
      return null;
    }

    const text = await response.text();
    return text.trim().length > 0 ? text : null;
  } catch {
    return null;
  }
}

function mapModuleListItem(value: unknown): ModuleListItem[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const id = readString(raw.id);
  if (!id) {
    return [];
  }

  return [
    {
      id,
      title: readString(raw.title) ?? "",
      grade: (raw.grade as ModuleListItem["grade"]) ?? 0,
      subject: readString(raw.subject) ?? "",
      sequenceOrder: typeof raw.sequenceOrder === "number" ? raw.sequenceOrder : 0,
      contentItemCount: typeof raw.contentItemCount === "number" ? raw.contentItemCount : 0,
    },
  ];
}

function mapModule(raw: Record<string, unknown>): ModuleDetail {
  const items = Array.isArray(raw.contentItems) ? raw.contentItems : [];
  return {
    id: readString(raw.id) ?? "",
    title: readString(raw.title) ?? "",
    grade: (raw.grade as ModuleDetail["grade"]) ?? 0,
    subject: readString(raw.subject) ?? "",
    sequenceOrder: typeof raw.sequenceOrder === "number" ? raw.sequenceOrder : 0,
    isRequired: Boolean(raw.isRequired),
    contentItems: items.flatMap(mapModuleItem),
  };
}

function mapModuleItem(value: unknown): ModuleContentItem[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const contentItemId = readString(raw.contentItemId);
  if (!contentItemId) {
    return [];
  }

  return [
    {
      contentItemId,
      sequenceOrder: typeof raw.sequenceOrder === "number" ? raw.sequenceOrder : 0,
      title: readString(raw.title) ?? "",
      type: parseContentType(raw.type),
    },
  ];
}

function mapContent(raw: Record<string, unknown>): ContentDetail {
  return {
    id: readString(raw.id) ?? "",
    title: readString(raw.title) ?? "",
    type: parseContentType(raw.type),
    language: readString(raw.language) ?? "en",
    fileSizeBytes: typeof raw.fileSizeBytes === "number" ? raw.fileSizeBytes : 0,
    durationSeconds: typeof raw.durationSeconds === "number" ? raw.durationSeconds : null,
    downloadUrl: readString(raw.downloadUrl) ?? "",
  };
}

function mapCaption(value: unknown): CaptionTrack[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const trackId = readString(raw.trackId);
  const signedUrl = readString(raw.signedUrl);
  if (!trackId || !signedUrl) {
    return [];
  }

  return [
    {
      trackId,
      language: readString(raw.language) ?? "en",
      signedUrl,
    },
  ];
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}
