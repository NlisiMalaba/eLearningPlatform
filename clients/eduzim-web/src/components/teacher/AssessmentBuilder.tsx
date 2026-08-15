"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { QuestionEditor } from "@/components/teacher/QuestionEditor";
import type { MessageKey } from "@/lib/i18n/messages";
import { t } from "@/lib/i18n/t";
import {
  createQuestionDraft,
  validateAssessmentDraft,
  type AssessmentDraft,
  type AssessmentDraftIssue,
  type AssessmentQuestionDraft,
} from "@/lib/teacher/assessmentDraft";
import { createAssessment } from "@/lib/teacher/assessmentService";
import { listModules } from "@/lib/teacher/teacherContentService";

const ISSUE_KEYS: Record<AssessmentDraftIssue, MessageKey> = {
  title: "teacher.assessment.issue.title",
  module: "teacher.assessment.issue.module",
  questions: "teacher.assessment.issue.questions",
  points: "teacher.assessment.issue.points",
  time: "teacher.assessment.issue.time",
  "mc-options": "teacher.assessment.issue.mcOptions",
  "mc-correct": "teacher.assessment.issue.mcCorrect",
  "tf-correct": "teacher.assessment.issue.tfCorrect",
  short: "teacher.assessment.issue.short",
};

const EMPTY_DRAFT: AssessmentDraft = {
  title: "",
  moduleId: "",
  passingScorePercent: 60,
  timed: false,
  timeLimitMinutes: 15,
  questions: [createQuestionDraft("MultipleChoice")],
};

export function AssessmentBuilder() {
  const router = useRouter();
  const modulesQuery = useQuery({ queryKey: ["teacher-modules"], queryFn: listModules });
  const [draft, setDraft] = useState<AssessmentDraft>(EMPTY_DRAFT);
  const [busy, setBusy] = useState(false);
  const [issue, setIssue] = useState<AssessmentDraftIssue | "failed" | null>(null);

  async function onSubmit(): Promise<void> {
    const invalid = validateAssessmentDraft(draft);
    if (invalid) {
      setIssue(invalid);
      return;
    }

    setBusy(true);
    setIssue(null);
    try {
      const id = await createAssessment(draft);
      router.push(`/teacher/assessments/${id}`);
    } catch {
      setIssue("failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h2 className="text-lg font-semibold">{t("teacher.assessment.create")}</h2>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.assessment.titleField")}
        <input
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={draft.title}
          onChange={(event) => setDraft({ ...draft, title: event.target.value })}
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.assessment.module")}
        <select
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={draft.moduleId}
          onChange={(event) => setDraft({ ...draft, moduleId: event.target.value })}
        >
          <option value="">{t("teacher.assessment.modulePlaceholder")}</option>
          {(modulesQuery.data ?? []).map((item) => (
            <option key={item.id} value={item.id}>
              {item.title}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.assessment.passing")}
        <input
          type="number"
          min={0}
          max={100}
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={draft.passingScorePercent}
          onChange={(event) =>
            setDraft({ ...draft, passingScorePercent: Number.parseInt(event.target.value, 10) || 0 })
          }
        />
      </label>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={draft.timed}
          onChange={(event) => setDraft({ ...draft, timed: event.target.checked })}
        />
        {t("teacher.assessment.timed")}
      </label>
      {draft.timed ? (
        <label className="flex flex-col gap-1 text-sm">
          {t("teacher.assessment.minutes")}
          <input
            type="number"
            min={1}
            max={1440}
            className="rounded-lg border border-zinc-300 px-3 py-2"
            value={draft.timeLimitMinutes}
            onChange={(event) =>
              setDraft({ ...draft, timeLimitMinutes: Number.parseInt(event.target.value, 10) || 1 })
            }
          />
        </label>
      ) : null}
      {draft.questions.map((question, index) => (
        <QuestionEditor
          key={question.id}
          index={index}
          question={question}
          onChange={(next) => replaceQuestion(draft, setDraft, next)}
          onRemove={() =>
            setDraft({ ...draft, questions: draft.questions.filter((item) => item.id !== question.id) })
          }
        />
      ))}
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          className="rounded-lg border border-zinc-300 px-3 py-1.5 text-sm"
          onClick={() => addQuestion(draft, setDraft, "MultipleChoice")}
        >
          {t("teacher.assessment.addMc")}
        </button>
        <button
          type="button"
          className="rounded-lg border border-zinc-300 px-3 py-1.5 text-sm"
          onClick={() => addQuestion(draft, setDraft, "TrueFalse")}
        >
          {t("teacher.assessment.addTf")}
        </button>
        <button
          type="button"
          className="rounded-lg border border-zinc-300 px-3 py-1.5 text-sm"
          onClick={() => addQuestion(draft, setDraft, "ShortAnswer")}
        >
          {t("teacher.assessment.addSa")}
        </button>
      </div>
      {issue ? (
        <p role="alert" className="text-sm text-red-700">
          {issue === "failed" ? t("teacher.assessment.createFailed") : t(ISSUE_KEYS[issue])}
        </p>
      ) : null}
      <button
        type="button"
        disabled={busy}
        onClick={() => void onSubmit()}
        className="self-start rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
      >
        {t("teacher.assessment.submit")}
      </button>
    </div>
  );
}

function replaceQuestion(
  draft: AssessmentDraft,
  setDraft: (draft: AssessmentDraft) => void,
  next: AssessmentQuestionDraft,
): void {
  setDraft({
    ...draft,
    questions: draft.questions.map((item) => (item.id === next.id ? next : item)),
  });
}

function addQuestion(
  draft: AssessmentDraft,
  setDraft: (draft: AssessmentDraft) => void,
  type: AssessmentQuestionDraft["type"],
): void {
  setDraft({ ...draft, questions: [...draft.questions, createQuestionDraft(type)] });
}
