import type { GradeAssessment } from "./assessment-types";

export function assessmentScore(item: GradeAssessment) {
  const value = item.values;
  const earned = number(value.assignmentScore) + number(value.midtermScore) + number(value.finalExamScore);
  const maximum = number(value.assignmentMaximum) + number(value.midtermMaximum) + number(value.finalExamMaximum);
  return maximum > 0 ? Math.round(earned / maximum * 10000) / 100 : 0;
}

export function gradeLetter(score: number, rules: Record<string, string>) {
  if (score >= number(rules.aMinimum)) return "A";
  if (score >= number(rules.bMinimum)) return "B";
  if (score >= number(rules.cMinimum)) return "C";
  if (score >= number(rules.dMinimum)) return "D";
  if (score >= number(rules.eMinimum)) return "E";
  return "F";
}

export function statusLabel(status: GradeAssessment["values"]["reviewStatus"]) {
  const labels: Record<GradeAssessment["values"]["reviewStatus"], string> = {
    SubmissionRequested: "Pending approval",
    SubmissionAuthorized: "Permission approved",
    Submitted: "Pending review",
    Pending: "Pending review",
    Approved: "Accepted",
    Rejected: "Rejected",
    ResubmitRequested: "Resubmission requested",
    ResubmitAuthorized: "Permission approved",
  };
  return labels[status];
}

export function statusTone(status: GradeAssessment["values"]["reviewStatus"]) {
  if (status === "Approved") return "approved";
  if (status === "Rejected" || status === "ResubmitRequested") return "rejected";
  if (status === "SubmissionAuthorized" || status === "ResubmitAuthorized") return "authorized";
  return "pending";
}

function number(value: string | undefined) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}
