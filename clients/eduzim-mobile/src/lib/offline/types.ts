export const OfflineSyncKinds = {
  ModuleProgress: "ModuleProgress",
  AssessmentAttempt: "AssessmentAttempt",
} as const;

export type OfflineSyncKind = (typeof OfflineSyncKinds)[keyof typeof OfflineSyncKinds];

export type OfflineSyncPayload = {
  kind: OfflineSyncKind;
  moduleId?: string;
  isCompleted?: boolean;
  completedAt?: string;
  timeOnTaskSeconds?: number;
  assessmentId?: string;
  attemptId?: string;
  scorePercent?: number;
  timeTakenSeconds?: number;
  submittedAt?: string;
};

export type OfflineSyncItem = {
  clientId: string;
  localTimestamp: string;
  payload: OfflineSyncPayload;
};

export type QueuedMutation = OfflineSyncItem & {
  id: string;
  studentId: string;
  queuedAt: string;
};

export type UploadOfflineQueueRequest = {
  studentId: string;
  items: OfflineSyncItem[];
};

export type UploadOfflineQueueResponse = {
  acceptedCount: number;
  syncedCount: number;
  conflictedCount: number;
};
