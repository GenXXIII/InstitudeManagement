import type { EnrollmentItem } from "@/features/enrollment/common/enrollment-types";
import type { GradeAssessment } from "./assessment-types";

export type CourseAssessmentGroup = {
  key: string;
  anchorId: string;
  teacherId: string;
  teacher: string;
  courseId: string;
  course: string;
  departmentId: string;
  department: string;
  year: string;
  shift: string;
  academicYear: string;
  term: string;
  status: GradeAssessment["values"]["reviewStatus"];
  version: number;
  submittedAtUtc: string;
  reviewedAtUtc: string;
  note: string;
  grades: GradeAssessment[];
};

export function groupCourseAssessments(rows: GradeAssessment[], enrollments: EnrollmentItem[]): CourseAssessmentGroup[] {
  const enrollmentByStudentPeriod = new Map(enrollments.map(item => [studentPeriodKey(item.id, item.values.academicYear, item.values.semester), item]));
  const groups = new Map<string, CourseAssessmentGroup>();
  for (const grade of rows.filter(item => Boolean(item.values.submittedByTeacherId))) {
    const value = grade.values;
    const enrollment = enrollmentByStudentPeriod.get(studentPeriodKey(value.studentId, value.academicYear, value.term));
    const year = enrollment?.values.year ?? "";
    const shift = enrollment?.values.shift ?? "";
    const key = [value.submittedByTeacherId, value.courseId, value.departmentId, year, shift, value.academicYear, value.term].join("|");
    const current = groups.get(key) ?? {
      key,
      anchorId: grade.id,
      teacherId: value.submittedByTeacherId,
      teacher: value.submittedByTeacher,
      courseId: value.courseId,
      course: value.course,
      departmentId: value.departmentId,
      department: value.department,
      year,
      shift,
      academicYear: value.academicYear,
      term: value.term,
      status: value.reviewStatus,
      version: Number(value.submissionVersion) || 1,
      submittedAtUtc: value.submittedAtUtc,
      reviewedAtUtc: value.reviewedAtUtc,
      note: value.reviewNote,
      grades: [],
    };
    current.grades.push(grade);
    current.version = Math.max(current.version, Number(value.submissionVersion) || 1);
    if (dateValue(value.submittedAtUtc) > dateValue(current.submittedAtUtc)) current.submittedAtUtc = value.submittedAtUtc;
    if (dateValue(value.reviewedAtUtc) > dateValue(current.reviewedAtUtc)) current.reviewedAtUtc = value.reviewedAtUtc;
    if (value.reviewNote) current.note = value.reviewNote;
    groups.set(key, current);
  }
  return [...groups.values()].map(group => ({
    ...group,
    status: aggregateStatus(group.grades),
    grades: group.grades.toSorted((left, right) => left.values.student.localeCompare(right.values.student, undefined, { numeric: true, sensitivity: "base" })),
  }));
}

export function assessmentGroupRouteId(group: CourseAssessmentGroup) {
  return group.anchorId;
}

function aggregateStatus(grades: GradeAssessment[]) {
  const statuses = [...new Set(grades.map(item => item.values.reviewStatus))];
  if (statuses.length === 1) return statuses[0];
  const priority: GradeAssessment["values"]["reviewStatus"][] = ["SubmissionRequested", "ResubmitRequested", "Submitted", "Pending", "SubmissionAuthorized", "ResubmitAuthorized", "Rejected", "Approved"];
  return priority.find(status => statuses.includes(status)) ?? "Pending";
}

function studentPeriodKey(studentId: string, academicYear: string, term: string) {
  return [studentId, academicYear, term].join("|");
}

function dateValue(value: string) {
  const parsed = new Date(value).valueOf();
  return Number.isNaN(parsed) ? 0 : parsed;
}
