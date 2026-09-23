import type { RecordItem } from "./history-types";

export type AssessmentHistoryGrade = {
  recordId: string;
  gradeCode: string;
  studentId: string;
  student: string;
  studentCode: string;
  courseId: string;
  courseCode: string;
  course: string;
  departmentId: string;
  department: string;
  yearLevel: string;
  shift: string;
  academicYear: string;
  term: string;
  attendanceScore: string;
  attendanceMaximum: string;
  assignmentScore: string;
  assignmentMaximum: string;
  midtermScore: string;
  midtermMaximum: string;
  finalExamScore: string;
  finalExamMaximum: string;
  score: string;
  grade: string;
  submittedByTeacherId: string;
  submittedByTeacher: string;
  reviewStatus: string;
  reviewNote: string;
  submissionVersion: string;
  submittedAtUtc: string;
  reviewedAtUtc: string;
};

export type AssessmentHistoryGroup = {
  key: string;
  routeId: string;
  courseCode: string;
  course: string;
  teacher: string;
  department: string;
  yearLevel: string;
  shift: string;
  academicYear: string;
  term: string;
  submissionVersion: number;
  submittedAtUtc: string;
  reviewedAtUtc: string;
  grades: AssessmentHistoryGrade[];
};

export function assessmentHistoryGroups(rows: RecordItem[]) {
  const groups = new Map<string, AssessmentHistoryGroup>();
  for (const row of rows.filter(item => item.type.toLowerCase() === "grade" && item.action === "Recorded")) {
    const value = gradeFrom(row);
    if (!value.courseId) continue;
    const key = [value.submittedByTeacherId, value.courseId, value.departmentId, value.yearLevel, value.shift, value.academicYear, value.term].join("|");
    const group = groups.get(key) ?? {
      key,
      routeId: value.recordId,
      courseCode: value.courseCode,
      course: value.course,
      teacher: value.submittedByTeacher || "Institute record",
      department: value.department,
      yearLevel: value.yearLevel,
      shift: value.shift,
      academicYear: value.academicYear,
      term: value.term,
      submissionVersion: Number(value.submissionVersion) || 1,
      submittedAtUtc: value.submittedAtUtc,
      reviewedAtUtc: value.reviewedAtUtc,
      grades: [],
    };
    group.grades.push(value);
    group.submissionVersion = Math.max(group.submissionVersion, Number(value.submissionVersion) || 1);
    if (dateValue(value.submittedAtUtc) > dateValue(group.submittedAtUtc)) group.submittedAtUtc = value.submittedAtUtc;
    if (dateValue(value.reviewedAtUtc) > dateValue(group.reviewedAtUtc)) group.reviewedAtUtc = value.reviewedAtUtc;
    groups.set(key, group);
  }
  return [...groups.values()].map(group => ({ ...group, grades: group.grades.toSorted((left, right) => left.student.localeCompare(right.student, undefined, { numeric: true, sensitivity: "base" })) }))
    .toSorted((left, right) => dateValue(right.reviewedAtUtc || right.submittedAtUtc) - dateValue(left.reviewedAtUtc || left.submittedAtUtc));
}

function gradeFrom(row: RecordItem): AssessmentHistoryGrade {
  let values: Record<string, unknown> = {};
  try { values = JSON.parse(row.details) as Record<string, unknown>; } catch { /* Legacy plain-text history is skipped by the missing courseId check. */ }
  const text = (key: string) => values[key] === null || values[key] === undefined ? "" : String(values[key]);
  return {
    recordId: row.resourceId ?? row.id,
    gradeCode: text("gradeCode"), studentId: text("studentId"), student: text("student"), studentCode: text("studentCode"),
    courseId: text("courseId"), courseCode: text("courseCode"), course: text("course"), departmentId: text("departmentId"), department: text("department"),
    yearLevel: text("yearLevel"), shift: text("shift"), academicYear: text("academicYear"), term: text("term"),
    attendanceScore: text("attendanceScore"), attendanceMaximum: text("attendanceMaximum"), assignmentScore: text("assignmentScore"), assignmentMaximum: text("assignmentMaximum"),
    midtermScore: text("midtermScore"), midtermMaximum: text("midtermMaximum"), finalExamScore: text("finalExamScore"), finalExamMaximum: text("finalExamMaximum"),
    score: text("score"), grade: text("grade"), submittedByTeacherId: text("submittedByTeacherId"), submittedByTeacher: text("submittedByTeacher"),
    reviewStatus: text("reviewStatus"), reviewNote: text("reviewNote"), submissionVersion: text("submissionVersion"), submittedAtUtc: text("submittedAtUtc"), reviewedAtUtc: text("reviewedAtUtc"),
  };
}

function dateValue(value: string) { const parsed = new Date(value).valueOf(); return Number.isNaN(parsed) ? 0 : parsed; }
