"use client";

import { t } from "@/lib/i18n/t";
import type { AssessmentQuestionDraft, AssessmentQuestionType } from "@/lib/teacher/assessmentDraft";

type QuestionEditorProps = {
  index: number;
  question: AssessmentQuestionDraft;
  onChange: (question: AssessmentQuestionDraft) => void;
  onRemove: () => void;
};

export function QuestionEditor({ index, question, onChange, onRemove }: QuestionEditorProps) {
  return (
    <fieldset className="flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white p-4">
      <legend className="px-1 text-sm font-semibold">
        {t("teacher.assessment.question")} {index + 1}
      </legend>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.assessment.questionType")}
        <select
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={question.type}
          onChange={(event) => onChange(withType(question, event.target.value as AssessmentQuestionType))}
        >
          <option value="MultipleChoice">{t("teacher.assessment.type.mc")}</option>
          <option value="TrueFalse">{t("teacher.assessment.type.tf")}</option>
          <option value="ShortAnswer">{t("teacher.assessment.type.sa")}</option>
        </select>
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.assessment.prompt")}
        <textarea
          className="rounded-lg border border-zinc-300 px-3 py-2"
          rows={2}
          value={question.text}
          onChange={(event) => onChange({ ...question, text: event.target.value })}
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.assessment.points")}
        <input
          type="number"
          min={1}
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={question.points}
          onChange={(event) => onChange({ ...question, points: Number.parseInt(event.target.value, 10) || 1 })}
        />
      </label>
      {question.type === "MultipleChoice" ? (
        <MultipleChoiceFields question={question} onChange={onChange} />
      ) : null}
      {question.type === "TrueFalse" ? (
        <TrueFalseFields question={question} onChange={onChange} />
      ) : null}
      {question.type === "ShortAnswer" ? (
        <label className="flex flex-col gap-1 text-sm">
          {t("teacher.assessment.correctShort")}
          <input
            className="rounded-lg border border-zinc-300 px-3 py-2"
            value={question.correctShortAnswer}
            onChange={(event) => onChange({ ...question, correctShortAnswer: event.target.value })}
          />
        </label>
      ) : null}
      <button type="button" onClick={onRemove} className="self-start text-sm text-red-700 underline">
        {t("teacher.assessment.removeQuestion")}
      </button>
    </fieldset>
  );
}

function MultipleChoiceFields({
  question,
  onChange,
}: {
  question: AssessmentQuestionDraft;
  onChange: (question: AssessmentQuestionDraft) => void;
}) {
  return (
    <div className="flex flex-col gap-2">
      {question.optionTexts.map((option, optionIndex) => (
        <label key={`${question.id}-opt-${optionIndex}`} className="flex items-center gap-2 text-sm">
          <input
            type="radio"
            name={`correct-${question.id}`}
            checked={question.correctOptionIndex === optionIndex}
            onChange={() => onChange({ ...question, correctOptionIndex: optionIndex })}
            aria-label={t("teacher.assessment.markCorrect")}
          />
          <input
            className="flex-1 rounded-lg border border-zinc-300 px-3 py-2"
            value={option}
            onChange={(event) =>
              onChange({
                ...question,
                optionTexts: question.optionTexts.map((item, i) => (i === optionIndex ? event.target.value : item)),
              })
            }
          />
        </label>
      ))}
      <button
        type="button"
        className="self-start text-sm text-[#0B6E4F] underline"
        onClick={() => onChange({ ...question, optionTexts: [...question.optionTexts, ""] })}
      >
        {t("teacher.assessment.addOption")}
      </button>
    </div>
  );
}

function TrueFalseFields({
  question,
  onChange,
}: {
  question: AssessmentQuestionDraft;
  onChange: (question: AssessmentQuestionDraft) => void;
}) {
  return (
    <div className="flex gap-4 text-sm">
      <label className="flex items-center gap-2">
        <input
          type="radio"
          name={`tf-${question.id}`}
          checked={question.correctOptionIndex === 0}
          onChange={() => onChange({ ...question, correctOptionIndex: 0 })}
        />
        {t("content.quiz.true")}
      </label>
      <label className="flex items-center gap-2">
        <input
          type="radio"
          name={`tf-${question.id}`}
          checked={question.correctOptionIndex === 1}
          onChange={() => onChange({ ...question, correctOptionIndex: 1 })}
        />
        {t("content.quiz.false")}
      </label>
    </div>
  );
}

function withType(question: AssessmentQuestionDraft, type: AssessmentQuestionType): AssessmentQuestionDraft {
  return {
    ...question,
    type,
    optionTexts: type === "MultipleChoice" ? (question.optionTexts.length >= 2 ? question.optionTexts : ["", ""]) : type === "TrueFalse" ? ["True", "False"] : [],
    correctOptionIndex: type === "ShortAnswer" ? null : (question.correctOptionIndex ?? 0),
    correctShortAnswer: type === "ShortAnswer" ? question.correctShortAnswer : "",
  };
}
