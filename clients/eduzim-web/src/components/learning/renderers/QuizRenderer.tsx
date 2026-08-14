"use client";

import { useMemo, useState } from "react";
import type { QuizPayload, QuizQuestion } from "@/lib/content/types";
import { t } from "@/lib/i18n/t";

type QuizRendererProps = {
  quiz: QuizPayload;
};

export function QuizRenderer({ quiz }: QuizRendererProps) {
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [checked, setChecked] = useState(false);

  const questions = quiz.questions;
  const canCheck = useMemo(
    () => questions.some((question) => hasKey(question)),
    [questions],
  );

  if (questions.length === 0) {
    return <p className="text-sm text-zinc-600">{t("content.quiz.empty")}</p>;
  }

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(event) => {
        event.preventDefault();
        setChecked(true);
      }}
      aria-label={t("content.quiz.label")}
    >
      {questions.map((question, index) => (
        <fieldset key={question.id} className="rounded-xl border border-zinc-200 bg-white p-4">
          <legend className="text-sm font-semibold">
            {t("content.quiz.prompt")} {index + 1}
          </legend>
          <p className="mt-2 text-base">{question.prompt}</p>
          <QuestionInput
            question={question}
            value={answers[question.id] ?? ""}
            onChange={(value) => {
              setChecked(false);
              setAnswers((current) => ({ ...current, [question.id]: value }));
            }}
          />
          {checked && hasKey(question) ? (
            <p className="mt-3 text-sm font-medium" role="status">
              {isCorrect(question, answers[question.id] ?? "")
                ? t("content.quiz.correct")
                : t("content.quiz.incorrect")}
            </p>
          ) : null}
        </fieldset>
      ))}
      {canCheck ? (
        <button
          type="submit"
          className="self-start rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white"
        >
          {t("content.quiz.submit")}
        </button>
      ) : null}
    </form>
  );
}

function QuestionInput({
  question,
  value,
  onChange,
}: {
  question: QuizQuestion;
  value: string;
  onChange: (value: string) => void;
}) {
  if (question.type === "ShortAnswer") {
    return (
      <input
        className="mt-3 w-full rounded-lg border border-zinc-300 px-3 py-2"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        aria-label={question.prompt}
      />
    );
  }

  const options =
    question.options.length > 0
      ? question.options
      : question.type === "TrueFalse"
        ? [t("content.quiz.true"), t("content.quiz.false")]
        : [];

  return (
    <div className="mt-3 flex flex-col gap-2">
      {options.map((option, index) => (
        <label key={`${question.id}-${index}`} className="flex cursor-pointer items-center gap-2 text-sm">
          <input
            type="radio"
            name={question.id}
            value={String(index)}
            checked={value === String(index)}
            onChange={() => onChange(String(index))}
          />
          {option}
        </label>
      ))}
    </div>
  );
}

function hasKey(question: QuizQuestion): boolean {
  return question.correctOptionIndex !== undefined || Boolean(question.correctShortAnswer);
}

function isCorrect(question: QuizQuestion, answer: string): boolean {
  if (question.type === "ShortAnswer") {
    return (
      answer.trim().toLowerCase() === (question.correctShortAnswer ?? "").trim().toLowerCase()
    );
  }

  return answer === String(question.correctOptionIndex);
}
