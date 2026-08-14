import type { ModuleContentItem } from "@/lib/content/types";
import {
  publishContent,
  setModuleContentItems,
  type ContentLibraryItem,
} from "@/lib/teacher/teacherContentService";

export function toggleAttachedId(ids: string[], id: string): string[] {
  return ids.includes(id) ? ids.filter((item) => item !== id) : [...ids, id];
}

export function moveAttachedId(ids: string[], id: string, direction: -1 | 1): string[] {
  const index = ids.indexOf(id);
  const next = index + direction;
  if (index < 0 || next < 0 || next >= ids.length) {
    return ids;
  }

  const copy = [...ids];
  const [removed] = copy.splice(index, 1);
  copy.splice(next, 0, removed);
  return copy;
}

export function attachedRowTitle(
  id: string,
  library: ContentLibraryItem[],
  items: ModuleContentItem[],
): string | undefined {
  return library.find((item) => item.id === id)?.title ?? items.find((item) => item.contentItemId === id)?.title;
}

export async function saveModuleSequence(moduleId: string, ids: string[]): Promise<void> {
  await setModuleContentItems(moduleId, ids);
}

export async function publishDraftItems(library: ContentLibraryItem[], ids: string[]): Promise<void> {
  const drafts = library.filter((item) => ids.includes(item.id) && item.status === "Draft");
  for (const item of drafts) {
    await publishContent(item.id);
  }
}
