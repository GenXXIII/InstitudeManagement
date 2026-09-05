import type { ClassroomItem } from "@/features/management/classrooms/classroom-types";
import type { CourseItem } from "@/features/management/courses/course-types";
import type { DepartmentItem } from "@/features/management/departments/department-types";
import type { StudentItem } from "@/features/management/students/student-types";
import type { TeacherItem } from "@/features/management/teachers/teacher-types";
import type { TimetableItem } from "@/features/timetable/timetable-types";
import type { EnrollmentItem } from "./common/enrollment-types";
import { enrollmentCohortKey, scheduleMatchesShift } from "./common/enrollment-relationships";

export type EnrollmentOverviewData = {
  management: {
    students: StudentItem[];
    teachers: TeacherItem[];
    courses: CourseItem[];
    classrooms: ClassroomItem[];
    timetable: TimetableItem[];
  };
  enrollment: {
    students: EnrollmentItem[];
    teachers: EnrollmentItem[];
    courses: EnrollmentItem[];
    classrooms: EnrollmentItem[];
    timetable: EnrollmentItem[];
  };
  departments: DepartmentItem[];
};

type CohortOverview = {
  key: string;
  departmentId: string;
  department: string;
  year: string;
  shift: string;
  students: number;
  courses: number;
  schedules: number;
  missingCourses: number;
};

export function buildEnrollmentOverview(data: EnrollmentOverviewData, selectedYear: string) {
  const sourceStudents = data.management.students.filter(item => item.values.status !== "Inactive" && matchesYear(item.values, selectedYear));
  const sourceTeachers = data.management.teachers.filter(item => item.values.status !== "Inactive");
  const sourceCourses = data.management.courses.filter(item => item.values.status !== "Inactive" && matchesYear(item.values, selectedYear));
  const sourceClassrooms = data.management.classrooms.filter(item => item.values.status !== "Inactive");
  const sourceTimetable = data.management.timetable.filter(item => item.values.status !== "Cancelled" && matchesYear(item.values, selectedYear));
  const activeStudents = data.enrollment.students.filter(item => item.values.status === "Active");
  const assignedTeachers = data.enrollment.teachers.filter(item => item.values.status === "Assigned");
  const activeCourses = data.enrollment.courses.filter(item => item.values.status === "Active");
  const assignedClassrooms = data.enrollment.classrooms.filter(item => item.values.status !== "Unassigned" && item.values.status !== "Removed");
  const enrolledTimetable = data.enrollment.timetable;
  const activeStudentIds = new Set(activeStudents.map(item => item.id));
  const activeCourseIds = new Set(activeCourses.map(item => item.id));
  const enrolledTimetableIds = new Set(enrolledTimetable.map(item => item.id));
  const grouped = new Map<string, { departmentId: string; department: string; year: string; shift: string; students: number }>();

  for (const student of activeStudents) {
    const values = student.values;
    if (!values.departmentId || !values.year || !values.shift) continue;
    const key = enrollmentCohortKey(values.departmentId, values.year, values.shift);
    const cohort = grouped.get(key) ?? { departmentId: values.departmentId, department: values.department || "Unassigned", year: values.year, shift: values.shift, students: 0 };
    cohort.students += 1;
    grouped.set(key, cohort);
  }

  const cohorts: CohortOverview[] = [...grouped.entries()].map(([key, cohort]) => {
    const courses = activeCourses.filter(course => course.values.departmentId === cohort.departmentId && course.values.year === cohort.year);
    const timetable = enrolledTimetable.filter(schedule => schedule.values.departmentId === cohort.departmentId && schedule.values.yearLevel === cohort.year && scheduleMatchesShift(schedule, cohort.shift));
    const scheduledCourseIds = new Set(timetable.map(schedule => schedule.values.courseId));
    return { key, ...cohort, courses: courses.length, schedules: timetable.length, missingCourses: courses.filter(course => !scheduledCourseIds.has(course.id)).length };
  }).toSorted((left, right) => left.department.localeCompare(right.department) || Number(left.year) - Number(right.year) || shiftOrder(left.shift) - shiftOrder(right.shift));

  const firstAttention = cohorts.find(cohort => !cohort.courses || cohort.missingCourses);
  return {
    sourceStudents, sourceTeachers, sourceCourses, sourceClassrooms, sourceTimetable,
    activeStudents, assignedTeachers, activeCourses, assignedClassrooms, enrolledTimetable, cohorts,
    missingStudents: sourceStudents.filter(item => !activeStudentIds.has(item.id)).length,
    missingCourses: sourceCourses.filter(item => !activeCourseIds.has(item.id)).length,
    missingTimetables: sourceTimetable.filter(item => !enrolledTimetableIds.has(item.id)).length,
    attentionCohorts: cohorts.filter(cohort => !cohort.courses || cohort.missingCourses).length,
    firstAttentionPath: firstAttention?.courses ? "/enrollment/timetable" : "/enrollment/courses",
    firstAttentionDepartmentId: firstAttention?.departmentId ?? "",
    firstAttentionYear: firstAttention?.year ?? "",
  };
}

export function scopedEnrollmentHref(pathname: string, departmentId: string, year: string) {
  const params = new URLSearchParams();
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  return `${pathname}${params.size ? `?${params}` : ""}`;
}

function matchesYear(values: Record<string, string>, year: string) {
  return !year || !values.year && !values.yearLevel || values.year === year || values.yearLevel === year;
}

function shiftOrder(shift: string) {
  return ["Morning", "Afternoon", "Evening", "Weekend"].indexOf(shift);
}
