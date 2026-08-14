export const CONTENT_TYPES = [
  "Video",
  "Pdf",
  "Audio",
  "Scene3D",
  "Animation",
  "Quiz",
  "Game",
] as const;

export type ContentType = (typeof CONTENT_TYPES)[number];

export const RENDERER_KINDS = [
  "video",
  "pdf",
  "audio",
  "scene3d",
  "quiz",
  "unsupported",
] as const;

export type RendererKind = (typeof RENDERER_KINDS)[number];

export type ModuleDetail = {
  id: string;
  title: string;
  grade: number | string;
  subject: string;
  sequenceOrder: number;
  isRequired: boolean;
  contentItems: ModuleContentItem[];
};

export type ModuleContentItem = {
  contentItemId: string;
  sequenceOrder: number;
  title: string;
  type: ContentType;
};

export type ContentDetail = {
  id: string;
  title: string;
  type: ContentType;
  language: string;
  fileSizeBytes: number;
  durationSeconds: number | null;
  downloadUrl: string;
};

export type CaptionTrack = {
  trackId: string;
  language: string;
  signedUrl: string;
};

export type TranscriptLink = {
  transcriptId: string;
  signedUrl: string;
};

export const QUIZ_QUESTION_TYPES = ["MultipleChoice", "TrueFalse", "ShortAnswer"] as const;

export type QuizQuestionType = (typeof QUIZ_QUESTION_TYPES)[number];

export type QuizQuestion = {
  id: string;
  type: QuizQuestionType;
  prompt: string;
  options: string[];
  correctOptionIndex?: number;
  correctShortAnswer?: string;
};

export type QuizPayload = {
  questions: QuizQuestion[];
};
