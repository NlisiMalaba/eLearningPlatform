"use client";

import type { InputHTMLAttributes, SelectHTMLAttributes } from "react";

type AuthTextFieldProps = {
  id: string;
  label: string;
  hint?: string;
  error?: string;
} & Omit<InputHTMLAttributes<HTMLInputElement>, "id">;

export function AuthTextField({
  id,
  label,
  hint,
  error,
  ...inputProps
}: AuthTextFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-zinc-800">
        {label}
      </label>
      <input
        id={id}
        aria-invalid={Boolean(error)}
        aria-describedby={describedBy}
        className="rounded-lg border border-zinc-300 bg-white px-3 py-2 text-base text-zinc-900 outline-none ring-[#0B6E4F] focus-visible:ring-2"
        {...inputProps}
      />
      {hint ? (
        <p id={hintId} className="text-sm text-zinc-500">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} role="alert" className="text-sm text-[#CE1126]">
          {error}
        </p>
      ) : null}
    </div>
  );
}

type AuthSelectFieldProps = {
  id: string;
  label: string;
} & Omit<SelectHTMLAttributes<HTMLSelectElement>, "id">;

export function AuthSelectField({ id, label, children, ...selectProps }: AuthSelectFieldProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium text-zinc-800">
        {label}
      </label>
      <select
        id={id}
        className="rounded-lg border border-zinc-300 bg-white px-3 py-2 text-base text-zinc-900 outline-none ring-[#0B6E4F] focus-visible:ring-2"
        {...selectProps}
      >
        {children}
      </select>
    </div>
  );
}
