import { useMemo, useState } from "react";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import { hasAnswerKey, isQuizAnswerCorrect } from "@/lib/content/quizScoring";
import type { QuizPayload, QuizQuestion } from "@/lib/content/types";
import { t } from "@/lib/i18n/t";
import { colors } from "@/theme";

type QuizRendererProps = {
  quiz: QuizPayload;
};

export function QuizRenderer({ quiz }: QuizRendererProps) {
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [checked, setChecked] = useState(false);
  const questions = quiz.questions;
  const canCheck = useMemo(() => questions.some((question) => hasAnswerKey(question)), [questions]);

  if (questions.length === 0) {
    return <Text style={styles.muted}>{t("content.quiz.empty")}</Text>;
  }

  return (
    <View accessibilityLabel={t("content.quiz.label")} style={styles.stack}>
      {questions.map((question, index) => (
        <View key={question.id} style={styles.card}>
          <Text style={styles.legend}>
            {t("content.quiz.prompt")} {index + 1}
          </Text>
          <Text style={styles.prompt}>{question.prompt}</Text>
          <QuestionInput
            question={question}
            value={answers[question.id] ?? ""}
            onChange={(value) => {
              setChecked(false);
              setAnswers((current) => ({ ...current, [question.id]: value }));
            }}
          />
          {checked && hasAnswerKey(question) ? (
            <Text accessibilityRole="text" style={styles.status}>
              {isQuizAnswerCorrect(question, answers[question.id] ?? "")
                ? t("content.quiz.correct")
                : t("content.quiz.incorrect")}
            </Text>
          ) : null}
        </View>
      ))}
      {canCheck ? (
        <Pressable
          accessibilityRole="button"
          onPress={() => setChecked(true)}
          style={styles.submit}
        >
          <Text style={styles.submitLabel}>{t("content.quiz.submit")}</Text>
        </Pressable>
      ) : null}
    </View>
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
      <TextInput
        accessibilityLabel={question.prompt}
        value={value}
        onChangeText={onChange}
        style={styles.input}
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
    <View style={styles.options}>
      {options.map((option, index) => {
        const selected = value === String(index);
        return (
          <Pressable
            key={`${question.id}-${index}`}
            accessibilityRole="radio"
            accessibilityState={{ selected }}
            onPress={() => onChange(String(index))}
            style={styles.option}
          >
            <View style={[styles.radio, selected ? styles.radioOn : null]} />
            <Text style={styles.optionLabel}>{option}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  stack: {
    gap: 16,
  },
  card: {
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    borderRadius: 12,
    padding: 16,
    gap: 8,
  },
  legend: {
    fontSize: 14,
    fontWeight: "600",
    color: colors.text,
  },
  prompt: {
    fontSize: 16,
    color: colors.text,
  },
  options: {
    gap: 8,
    marginTop: 4,
  },
  option: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    minHeight: 44,
  },
  radio: {
    width: 18,
    height: 18,
    borderRadius: 9,
    borderWidth: 2,
    borderColor: colors.brand,
  },
  radioOn: {
    backgroundColor: colors.brand,
  },
  optionLabel: {
    fontSize: 14,
    color: colors.text,
    flex: 1,
  },
  input: {
    marginTop: 8,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
  },
  status: {
    marginTop: 8,
    fontSize: 14,
    fontWeight: "600",
    color: colors.text,
  },
  submit: {
    alignSelf: "flex-start",
    backgroundColor: colors.brand,
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 10,
  },
  submitLabel: {
    color: colors.surface,
    fontWeight: "600",
    fontSize: 14,
  },
  muted: {
    color: colors.muted,
    fontSize: 14,
  },
});
