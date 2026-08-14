import { apiFetch } from "@/lib/api/client";
import { parseContentType } from "@/lib/content/parseContentType";
import type { ContentType, ModuleDetail } from "@/lib/content/types";
import { getModule } from "@/lib/content/contentService";
import { parseGradeLevel, gradeLevelIndex, type GradeLevel } from "@/lib/teacher/grades";

export type ContentLibraryItem = {
  id: string;
  title: string;
  type: ContentType;
  status: "Draft" | "Published";
  fileSizeBytes: number;
};

export type ModuleListItem = {
  id: string;
  title: string;
  grade: GradeLevel;
  subject: string;
  sequenceOrder: number;
  isRequired: boolean;
  contentItemCount: number;
};

export async function listContent(): Promise<ContentLibraryItem[]> {
  const raw = await apiFetch<unknown[]>("/api/v1/content");
  return Array.isArray(raw) ? raw.flatMap(mapLibraryItem) : [];
}

export async function uploadContent(input: {
  title: string;
  type: ContentType;
  file: File;
}): Promise<string> {
  const body = new FormData();
  body.append("title", input.title);
  body.append("type", input.type);
  body.append("language", "en");
  body.append("file", input.file);
  const raw = await apiFetch<Record<string, unknown>>("/api/v1/content/upload", {
    method: "POST",
    body,
  });
  return readString(raw.contentId) ?? "";
}

export async function publishContent(contentId: string): Promise<void> {
  await apiFetch(`/api/v1/content/${contentId}/publish`, { method: "POST" });
}

export async function listModules(): Promise<ModuleListItem[]> {
  const raw = await apiFetch<unknown[]>("/api/v1/modules");
  return Array.isArray(raw) ? raw.flatMap(mapModuleListItem) : [];
}

export async function createModule(input: {
  title: string;
  grade: GradeLevel;
  subject: string;
  sequenceOrder: number;
}): Promise<string> {
  const raw = await apiFetch<Record<string, unknown>>("/api/v1/modules", {
    method: "POST",
    body: JSON.stringify({
      title: input.title,
      grade: gradeLevelIndex(input.grade),
      subject: input.subject,
      sequenceOrder: input.sequenceOrder,
      isRequired: true,
    }),
  });
  return readString(raw.moduleId) ?? "";
}

export async function setModuleContentItems(
  moduleId: string,
  contentItemIds: string[],
): Promise<ModuleDetail> {
  await apiFetch(`/api/v1/modules/${moduleId}/content-items`, {
    method: "PUT",
    body: JSON.stringify({ contentItemIds }),
  });
  return getModule(moduleId);
}

export async function submitMarketplacePack(input: {
  title: string;
  description: string;
  contentItemIds: string[];
}): Promise<void> {
  await apiFetch("/api/v1/marketplace/packs", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

function mapLibraryItem(value: unknown): ContentLibraryItem[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const id = readString(raw.id);
  const title = readString(raw.title);
  if (!id || !title) {
    return [];
  }

  const status = parseStatus(raw.status);
  if (!status) {
    return [];
  }

  return [
    {
      id,
      title,
      type: parseContentType(raw.type),
      status,
      fileSizeBytes: typeof raw.fileSizeBytes === "number" ? raw.fileSizeBytes : 0,
    },
  ];
}

function mapModuleListItem(value: unknown): ModuleListItem[] {
  if (!value || typeof value !== "object") {
    return [];
  }

  const raw = value as Record<string, unknown>;
  const id = readString(raw.id);
  const title = readString(raw.title);
  const subject = readString(raw.subject);
  if (!id || !title || !subject) {
    return [];
  }

  return [
    {
      id,
      title,
      grade: parseGradeLevel(raw.grade),
      subject,
      sequenceOrder: typeof raw.sequenceOrder === "number" ? raw.sequenceOrder : 0,
      isRequired: raw.isRequired === true,
      contentItemCount: typeof raw.contentItemCount === "number" ? raw.contentItemCount : 0,
    },
  ];
}

function parseStatus(value: unknown): "Draft" | "Published" | null {
  if (value === 0 || value === "Draft") {
    return "Draft";
  }

  if (value === 1 || value === "Published") {
    return "Published";
  }

  return null;
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}
