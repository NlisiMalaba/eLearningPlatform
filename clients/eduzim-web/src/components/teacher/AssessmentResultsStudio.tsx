"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { AssignAssessmentForm } from "@/components/teacher/AssignAssessmentForm";
import { ClassResultsTable } from "@/components/teacher/ClassResultsTable";
import { t } from "@/lib/i18n/t";
import { getClassResults, listAssessments, listSchoolClasses } from "@/lib/teacher/assessmentService";

type AssessmentResultsStudioProps = {
  assessmentId: string;
};

export function AssessmentResultsStudio({ assessmentId }: AssessmentResultsStudioProps) {
  const assessmentsQuery = useQuery({
    queryKey: ["teacher-assessments"],
    queryFn: listAssessments,
  });
  const classesQuery = useQuery({
    queryKey: ["teacher-classes"],
    queryFn: listSchoolClasses,
  });
  const [schoolClassId, setSchoolClassId] = useState<string>("");
  const resultsQuery = useQuery({
    queryKey: ["teacher-class-results", assessmentId, schoolClassId],
    queryFn: () => getClassResults(assessmentId, schoolClassId),
    enabled: schoolClassId.length > 0,
  });

  const assessment = (assessmentsQuery.data ?? []).find((item) => item.id === assessmentId);
  const classes = classesQuery.data ?? [];

  return (
    <div className="flex flex-col gap-8">
      <header>
        <p>
          <a href="/teacher/assessments" className="text-sm text-[#0B6E4F] underline">
            {t("teacher.assessment.back")}
          </a>
        </p>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight">
          {assessment?.title ?? t("teacher.assessment.resultsTitle")}
        </h1>
        <p className="mt-2 max-w-xl text-zinc-600">{t("teacher.results.subtitle")}</p>
      </header>
      {classesQuery.isError ? <p role="alert">{t("teacher.assessment.classesError")}</p> : null}
      <AssignAssessmentForm
        assessmentId={assessmentId}
        classes={classes}
        onAssigned={(id) => {
          setSchoolClassId(id);
          void resultsQuery.refetch();
        }}
      />
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.results.viewClass")}
        <select
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={schoolClassId}
          onChange={(event) => setSchoolClassId(event.target.value)}
        >
          <option value="">{t("teacher.results.pickClass")}</option>
          {classes.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
      </label>
      {resultsQuery.isError ? (
        <p role="alert">{t("teacher.results.error")}</p>
      ) : resultsQuery.isFetching && !resultsQuery.data ? (
        <p>{t("teacher.results.loading")}</p>
      ) : resultsQuery.data ? (
        <ClassResultsTable results={resultsQuery.data} />
      ) : null}
    </div>
  );
}
